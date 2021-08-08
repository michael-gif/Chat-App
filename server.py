import socket
from datetime import datetime
from threading import Thread

HOST = '0.0.0.0'
PORT = 42069

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
server_socket.bind((HOST, PORT))
server_socket.listen()

clients = []

def get_date_now():
   return datetime.now().strftime('%Y-%m-%d %H:%M:%S')

def receive_message(cs):
    while True:
        try:
            message = cs.recv(1024).decode()
            if not message:
               raise Exception('')
        except:
            # client disconnected
            print(f'[{get_date_now()}] Lost connection with {ca[0]}:{ca[1]}')
            clients.remove(cs)
            break

        for client in clients:
            client.send(message.encode('utf-8'))

print(f"[{get_date_now()}] Server opened on {HOST}:{PORT}")

while True:
    # waits for incoming connection, only proceeds when it gets one
    cs, ca = server_socket.accept()

    print(f"[{get_date_now()}] Connected with {ca[0]}:{ca[1]}")
    clients.append(cs)

    # make thread for client that listens for messages
    client_thread = Thread(target=receive_message, args=(cs,))
    client_thread.daemon = True
    client_thread.start()
