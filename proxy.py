import socket
import json
from datetime import datetime
from threading import Thread

# proxy host and port
HOST = '0.0.0.0'
PORT = 1738

# server host and port
SERVER_HOST = '127.0.0.1'
SERVER_PORT = 42069

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
server_socket.bind((HOST, PORT))
server_socket.listen()

proxy_connections = {}

def get_date_now():
   return datetime.now().strftime('%Y-%m-%d %H:%M:%S')

def receive_forward_message(crs, ca, css):
    while True:
        try:
            forward = crs.recv(1024).decode()
            if not forward:
               raise Exception('')
        except:
            # client disconnected
            print(f'[{get_date_now()}] Lost connection with {ca[0]}:{ca[1]}')
            css.close()
            break

        try:
            # forward any messages to the server using the associated socket
            css.send(forward.encode())
        except:
            # Server disconnected
            message = {
                "username": '[PROXY]',
                "time": get_date_now(),
                "message": 'No reponse from server'
            }
            crs.send(json.dumps(message).encode())

def receive_backward_message(crs, ca, css):
    while True:
        try:
            backward = css.recv(1024)
            if not backward:
               raise Exception('')
        except:
            # Server disconnected
            print(f'[{get_date_now()}] Disconnected {ca[0]}:{ca[1]} from {SERVER_HOST}:{SERVER_PORT}')
            break

        # forward any messages to the client using the associated socket
        crs.send(backward)

print(f"[{get_date_now()}] Proxy opened on {HOST}:{PORT}")

while True:
    # waits for incoming connection, only proceeds when it gets one
    client_receive_socket, client_address = server_socket.accept()

    client_send_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        # Connect to server
        client_send_socket.connect((SERVER_HOST, SERVER_PORT))
        print(f"[{get_date_now()}] Connected {client_address[0]}:{client_address[1]} to {SERVER_HOST}:{SERVER_PORT}")
    except:
        # Couldn't connect to server
        message = {
                "username": '[PROXY]',
                "time": get_date_now(),
                "message": 'Failed to connect to server'
            }
        client_receive_socket.send(json.dumps(message).encode())
        print(f"[{get_date_now()}] Failed to connect {client_address[0]}:{client_address[1]} to {SERVER_HOST}:{SERVER_PORT}")

    # make thread to forward client messages
    forward_thread = Thread(target=receive_forward_message, args=(client_receive_socket, client_address, client_send_socket))
    forward_thread.daemon = True
    forward_thread.start()

    # make a thread to forward server messages
    backward_thread = Thread(target=receive_backward_message, args=(client_receive_socket, client_address, client_send_socket))
    backward_thread.daemon = True
    backward_thread.start()
