from tkinter import *

def rgb(r, g, b):
    color = (r, g, b)
    return "#%02x%02x%02x" % color

def send_message(event):
    chatroom_inner.config(state='normal')
    chatroom_inner.insert(END, text_box_entry.get() + '\n')
    chatroom_inner.config(state='disabled')
    chatroom_inner.see(END)
    text_box_entry.delete(0, END)

root = Tk()
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

mainloop()
