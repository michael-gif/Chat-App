import socket
import argparse
import json
import sys
from datetime import datetime
from threading import Thread
from gui import *

HOST = "127.0.0.1"
PORT = 1738
CONNECTED = False

client_socket = None

parser = argparse.ArgumentParser()
parser.add_argument("--username")
args = parser.parse_args()
username = args.username

online_users = []
online_users_queue = []

# message to be sent
outbound_message_queue = []
# recevied messages to be displayed
inbound_message_queue = []


def get_date_now():
    return datetime.now().strftime("%Y-%m-%d %H:%M:%S")


def connect_to_server(username):
    global client_socket, CONNECTED
    try:
        client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        client_socket.connect((HOST, PORT))
        inbound_message_queue.append(f"Connected to -> [{HOST}:{PORT}]")
        CONNECTED = True
        if username == None:
            username = 'None'
        message = {
            "new_connection": username
        }
        client_socket.send(json.dumps(message).encode())
    except Exception as e:
        print(e)
        inbound_message_queue.append(f"[SERVER] -> Connection refused")
    return CONNECTED


def listen_for_messages(cs):
    global CONNECTED, inbound_message_queue, online_users, online_users_queue
    while CONNECTED:
        try:
            message_raw = cs.recv(1024).decode("utf-8")
            message_json = json.loads(message_raw)
            if "connection" in message_json:
                online_users_queue = message_json['connection']
            else:
                if message_json['username'] == '[PROXY]':
                    inbound_message_queue.append(f"[{message_json['time']}] {message_json['username']} -> {message_json['message']}")
                else:
                    inbound_message_queue.append(f"[{message_json['time']}] {message_json['username']}: {message_json['message']}")
        except Exception as e:
            print(e)
            inbound_message_queue.append(f"[SERVER] -> No response")
            CONNECTED = False


def loop():
    global CONNECTED, listen_thread, client_socket
    while True:
        if not outbound_message_queue:
            continue
        for message_raw in outbound_message_queue:
            if not message_raw:
                continue
            if message_raw[0] == '/':
                command = message_raw[1:]
                parts = command.split(' ')
                keyword = parts[0]
                if keyword == "kill":
                    if CONNECTED:
                        client_socket.close()
                        inbound_message_queue.append(f"Disconnected from {HOST}:{PORT}")
                        CONNECTED = False
                    else:
                        inbound_message_queue.append(f"Already disconnected from server")
                elif keyword == "reconnect":
                    if connect_to_server(username):
                        listen_thread = Thread(target=listen_for_messages, args=(client_socket,))
                        listen_thread.daemon = True
                        listen_thread.start()
                else:
                    inbound_message_queue.append(f"Unknown command '{keyword}'")
            else:
                try:
                    message = {
                        "username": username,
                        "time": get_date_now(),
                        "message": message_raw,
                    }
                    message_json = json.dumps(message)
                    client_socket.send(message_json.encode())
                except:
                    inbound_message_queue.append(f"[SERVER] -> No response")
        outbound_message_queue.clear()


connect_to_server(username)

listen_thread = Thread(target=listen_for_messages, args=(client_socket,))
listen_thread.daemon = True
listen_thread.start()

loop_thread = Thread(target=loop)
loop_thread.daemon = True
loop_thread.start()

gui = SpiceGUI()

while True:
    gui.update(inbound_message_queue, online_users_queue, online_users)
    outbound_message_queue = gui.get_outbound_messages()
