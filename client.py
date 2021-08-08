import socket
import argparse
import json
from datetime import datetime
from threading import Thread
from tkinter import *

HOST = '192.168.1.146'
PORT = 42069
CONNECTED = False

client_socket = None

parser = argparse.ArgumentParser()
parser.add_argument('--username')
args = parser.parse_args()
username = args.username

message_queue = []
received_message_queue = []

def get_date_now():
   return datetime.now().strftime('%Y-%m-%d %H:%M:%S')

def connect_to_server():
    global client_socket, CONNECTED
    try:
        client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        client_socket.connect((HOST, PORT))
        print(f'Connected to -> [{HOST}:{PORT}]')
        received_message_queue.append(f'Connected to -> [{HOST}:{PORT}]')
        CONNECTED = True
    except Exception as e:
        print(f'[{HOST}:{PORT}] -> Connection refused')
    return CONNECTED

def listen_for_messages(cs):
    global CONNECTED, received_message_queue
    while CONNECTED:
        try:
            message = cs.recv(1024).decode('utf-8')
            message = json.loads(message)
            print(f"[{message['time']}] {message['username']}: {message['message']}")
            received_message_queue.append(f"[{message['time']}] {message['username']}: {message['message']}")
        except:
            print(f'[{HOST}:{PORT}] -> No response from server')
            received_message_queue.append(f'[{HOST}:{PORT}] -> No response from server')
            CONNECTED = False

def main():
   global CONNECTED, listen_thread, client_socket
   while True:
       if not message_queue:
           continue
       for message_raw in message_queue:
           print(message_raw)
           if message_raw == '/kill':
               client_socket.close()
               print(f'Disconnected from {HOST}:{PORT}')
               received_message_queue.append(f'Disconnected from {HOST}:{PORT}')
               CONNECTED = False
           elif message_raw  == '/reconnect':
               if connect_to_server():
                   listen_thread = Thread(target=listen_for_messages, args=(client_socket,))
                   listen_thread.daemon = True
                   listen_thread.start()
           else:
              try:
                  message = {'username': username, 'time': get_date_now(), 'message': message_raw}
                  message = json.dumps(message)
                  client_socket.send(message.encode())
              except:
                  print(f'[{HOST}:{PORT}] -> No response from server')
           message_queue.pop(0)

connect_to_server()

listen_thread = Thread(target=listen_for_messages, args=(client_socket,))
listen_thread.daemon = True
listen_thread.start()

main_thread = Thread(target=main)
main_thread.daemon = True
main_thread.start()

root = Tk()

def rgb(r, g, b):
    color = (r, g, b)
    return "#%02x%02x%02x" % color

def send_message(event):
    message = text_box_entry.get()
    #chatroom_inner.config(state='normal')
    #chatroom_inner.insert(END, message + '\n')
    message_queue.append(message)
    #chatroom_inner.config(state='disabled')
    #chatroom_inner.see(END)
    text_box_entry.delete(0, END)

def receive_message_gui():
    global received_message_queue
    if received_message_queue:
        for message in received_message_queue:
            chatroom_inner.config(state='normal')
            chatroom_inner.insert(END, message + '\n')
            chatroom_inner.config(state='disabled')
            chatroom_inner.see(END)
            chatroom_inner.update()
            received_message_queue.pop(0)

root.title('Spice')

screen_width = root.winfo_screenwidth()
screen_height = root.winfo_screenheight()
root.attributes('-fullscreen', True)

screen_width = root.winfo_screenwidth()
screen_height = root.winfo_screenheight()

top_bar = Frame(root, bg=rgb(32, 34, 37))
top_bar.place(x=0, y=0, width=screen_width, height=25)

logo = Label(top_bar, text='Spice', fg='white', bg=rgb(32, 34, 37), font=('Comic Sans', 10))
logo.place(x=4, y=2)

chatroom = Frame(root, bg=rgb(54, 57, 63))
chatroom.place(x=0, y=25, width=screen_width - 240, height=screen_height - 25)
chatroom.update()

chatroom_inner = Text(chatroom, bg=rgb(54, 57, 63), fg='white', state='disabled')
chatroom_inner.place(x=16, y=16, width=chatroom.winfo_width() - 32, height=chatroom.winfo_height() - 108)
chatroom_inner.update()

members = Frame(root, bg=rgb(47, 49, 54))
members.place(x=screen_width - 240, y=25, width=240, height=screen_height - 25)
members.update()
members_label = Label(members, text='Members', bg=rgb(47, 49, 54), fg='white', font=('Comic Sans', 14))
members_label.place(x=10, y=10, width=members.winfo_width() - 20)

text_box_color = rgb(64, 68, 75)
text_box_container = Frame(chatroom, bg=text_box_color)
text_box_container.place(x=16, y=chatroom.winfo_height() - 68, width=chatroom.winfo_width() - 32, height=44)
text_box_container.update()
#text_box_label = Label(text_box_container, text='Message all', bg=text_box_color, fg=rgb(104, 115, 106), font=('Comic Sans', 12))
#text_box_label.place(x=10, y=10)
text_box_entry = Entry(text_box_container, bg=text_box_color, fg='white', insertbackground='white')
text_box_entry.place(x=10, y=10, width=text_box_container.winfo_width() - 20, height=25)
text_box_entry.bind('<Return>', send_message)
text_box_entry.focus_set()

while True:
    receive_message_gui()
    root.update_idletasks()
    root.update()

