import socket
import argparse
import json
from datetime import datetime
from threading import Thread
from tkinter import *

HOST = "192.168.1.146"
PORT = 42069
CONNECTED = False

client_socket = None

parser = argparse.ArgumentParser()
parser.add_argument("--username")
args = parser.parse_args()
username = args.username

# message to be sent
message_queue = []
# recevied messages to be displayed
received_message_queue = []


def get_date_now():
    return datetime.now().strftime("%Y-%m-%d %H:%M:%S")


def connect_to_server():
    global client_socket, CONNECTED
    try:
        client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        client_socket.connect((HOST, PORT))
        received_message_queue.append(f"Connected to -> [{HOST}:{PORT}]")
        CONNECTED = True
    except Exception as e:
        received_message_queue.append(f"[SERVER] -> Connection refused")
    return CONNECTED


def listen_for_messages(cs):
    global CONNECTED, received_message_queue
    while CONNECTED:
        try:
            message = cs.recv(1024).decode("utf-8")
            message = json.loads(message)
            received_message_queue.append(f"[{message['time']}] {message['username']}: {message['message']}")
        except:
            received_message_queue.append(f"[SERVER] -> No response")
            CONNECTED = False


def loop():
    global CONNECTED, listen_thread, client_socket
    while True:
        if not message_queue:
            continue
        for message_raw in message_queue:
            if not message_raw:
                message_queue.pop(0)
                continue
            if message_raw[0] == '/':
                command = message_raw[1:]
                parts = command.split(' ')
                keyword = parts[0]
                if keyword == "kill":
                    if CONNECTED:
                        client_socket.close()
                        received_message_queue.append(f"Disconnected from {HOST}:{PORT}")
                        CONNECTED = False
                    else:
                        received_message_queue.append(f"Already disconnected from server")
                elif keyword == "reconnect":
                    if connect_to_server():
                        listen_thread = Thread(target=listen_for_messages, args=(client_socket,))
                        listen_thread.daemon = True
                        listen_thread.start()
                else:
                    received_message_queue.append(f"Unknown command '{keyword}'")
            else:
                print(message)
                try:
                    message = {
                        "username": username,
                        "time": get_date_now(),
                        "message": message_raw,
                    }
                    message = json.dumps(message)
                    client_socket.send(message.encode())
                except:
                    received_message_queue.append(f"[SERVER] -> No response")
            message_queue.pop(0)


connect_to_server()

listen_thread = Thread(target=listen_for_messages, args=(client_socket,))
listen_thread.daemon = True
listen_thread.start()

loop_thread = Thread(target=loop)
loop_thread.daemon = True
loop_thread.start()


#----------------
# GUI
#----------------
def rgb(r, g, b):
    color = (r, g, b)
    return "#%02x%02x%02x" % color

def send_message(event):
    message = text_box_entry.get()
    message_queue.append(message)
    text_box_entry.delete(0, END)

def receive_message_gui():
    global received_message_queue
    if received_message_queue:
        for message in received_message_queue:
            chatroom_inner.config(state='normal')
            chatroom_inner.insert(END, message + '\n')
            print(message)
            chatroom_inner.config(state='disabled')
            chatroom_inner.see(END)
            chatroom_inner.update()
            received_message_queue.pop(0)

def on_enter(event):
    #event.widget['background'] = rgb(40, 43, 46)
    name = str(event.widget)
    name = name.split('.')[-1]
    event.widget['image'] = images_hover[name]

def on_leave(event):
    #event.widget['background'] = rgb(32, 34, 37)
    name = str(event.widget)
    name = name.split('.')[-1]
    event.widget['image'] = images_idle[name]

def exit_button_hover(event):
    event.widget['background'] = rgb()

def minimise():
    root.wm_state('iconic')

def maximise():
    root.attributes('-fullscreen', True)

def close_window():
    root.quit()
    root.destroy()

root = Tk()
root.title('Spice')

screen_width = root.winfo_screenwidth()
screen_height = root.winfo_screenheight()
root.attributes('-fullscreen', True)

screen_width = root.winfo_screenwidth()
screen_height = root.winfo_screenheight()

#----------------Title Bar----------------#
title_bar = Frame(root, bg=rgb(32, 34, 37))
title_bar.place(x=0, y=0, width=screen_width, height=25)
title_bar.update()

logo = Label(title_bar, text='Spice', fg='white', bg=rgb(32, 34, 37), font=('Comic Sans', 10))
logo.place(x=4, y=2)

images_idle = {
    "minimise": PhotoImage(file="./resources/minimise_idle.png"),
    "maximise": PhotoImage(file="./resources/maximise_idle.png"),
    "exit": PhotoImage(file="./resources/exit_idle.png"),
}

images_hover = {
    "minimise": PhotoImage(file="./resources/minimise_hover.png"),
    "maximise": PhotoImage(file="./resources/maximise_hover.png"),
    "exit": PhotoImage(file="./resources/exit_hover.png"),
}

minimise_button = Button(title_bar, bg=rgb(32, 34, 37), activebackground=rgb(43, 46, 50), highlightthickness=0, bd=0, image=images_idle['minimise'], name='minimise', command=minimise)
minimise_button.place(x=title_bar.winfo_width() - 84, y=0, width=28, height=25)
minimise_button.bind('<Enter>', on_enter)
minimise_button.bind('<Leave>', on_leave)
maximise_button = Button(title_bar, bg=rgb(32, 34, 37), activebackground=rgb(43, 46, 50), highlightthickness=0, bd=0, image=images_idle['maximise'], name='maximise', command=maximise)
maximise_button.place(x=title_bar.winfo_width() - 56, y=0, width=28, height=25)
maximise_button.bind('<Enter>', on_enter)
maximise_button.bind('<Leave>', on_leave)
exit_button = Button(title_bar, bg=rgb(32, 34, 37), activebackground=rgb(43, 46, 50), highlightthickness=0, bd=0, image=images_idle['exit'], name='exit', command=close_window)
exit_button.place(x=title_bar.winfo_width() - 28, y=0, width=28, height=25)
exit_button.bind('<Enter>', on_enter)
exit_button.bind('<Leave>', on_leave)

#----------------Received Message Area----------------#
chatroom = Frame(root, bg=rgb(54, 57, 63))
chatroom.place(x=0, y=25, width=screen_width - 240, height=screen_height - 25)
chatroom.update()

chatroom_inner = Text(chatroom, bg=rgb(54, 57, 63), fg='white', state='disabled')
chatroom_inner.place(x=16, y=16, width=chatroom.winfo_width() - 32, height=chatroom.winfo_height() - 108)
chatroom_inner.update()

#----------------Members Panel----------------#
members = Frame(root, bg=rgb(47, 49, 54))
members.place(x=screen_width - 240, y=25, width=240, height=screen_height - 25)
members.update()
members_label = Label(members, text='Members', bg=rgb(47, 49, 54), fg='white', font=('Comic Sans', 14))
members_label.place(x=10, y=10, width=members.winfo_width() - 20)

#----------------User Input----------------#
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

#----------------mainloop----------------#
while True:
    receive_message_gui()
    root.update_idletasks()
    root.update()

