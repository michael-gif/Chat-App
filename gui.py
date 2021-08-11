from tkinter import *
from win32api import GetMonitorInfo, MonitorFromPoint

class SpiceGUI:
    def __init__(self):
        '''
        self.root = Tk()
        self.root.title('Spice')
        self.root.state('zoomed')
        self.root.update()
        screen_width = self.root.winfo_screenwidth()
        screen_height = self.root.winfo_screenheight()
        '''

        self.outbound_message_queue = []
        
        self.window = Tk()
        self.window.title('Spice')
        self.window.attributes('-alpha', 0.0)
        self.window.iconify()
        self.window.update()

        self.window.bind("<Map>", self.toggle_window_visibility)
        self.window.bind("<Unmap>", self.toggle_window_visibility)

        self.root = Toplevel(self.window)
        self.root.update()

        screen_width = self.window.winfo_screenwidth()
        screen_height = self.window.winfo_screenheight()
        taskbar_height = self.get_taskbar_height()
        self.root.geometry(f"{screen_width}x{screen_height - taskbar_height}+0+0")
        self.root.overrideredirect(1)

        #----------------Title Bar----------------#
        title_bar = Frame(self.root, bg=self.rgb(32, 34, 37))
        title_bar.place(x=0, y=0, width=screen_width, height=25)
        title_bar.update()

        logo = Label(title_bar, text='Spice', fg='white', bg=self.rgb(32, 34, 37), font=('Comic Sans', 10))
        logo.place(x=4, y=2)

        self.images_idle = {
            "minimise": PhotoImage(file="./resources/minimise_idle.png"),
            "maximise": PhotoImage(file="./resources/maximise_idle.png"),
            "exit": PhotoImage(file="./resources/exit_idle.png"),
        }

        self.images_hover = {
            "minimise": PhotoImage(file="./resources/minimise_hover.png"),
            "maximise": PhotoImage(file="./resources/maximise_hover.png"),
            "exit": PhotoImage(file="./resources/exit_hover.png"),
        }

        minimise_button = Button(title_bar, bg=self.rgb(32, 34, 37), activebackground=self.rgb(43, 46, 50), highlightthickness=0, bd=0, image=self.images_idle['minimise'], name='minimise', command=self.minimise)
        minimise_button.place(x=title_bar.winfo_width() - 84, y=0, width=28, height=25)
        minimise_button.bind('<Enter>', self.titlebar_button_enter)
        minimise_button.bind('<Leave>', self.titlebar_button_leave)
        maximise_button = Button(title_bar, bg=self.rgb(32, 34, 37), activebackground=self.rgb(43, 46, 50), highlightthickness=0, bd=0, image=self.images_idle['maximise'], name='maximise', command=self.maximise)
        maximise_button.place(x=title_bar.winfo_width() - 56, y=0, width=28, height=25)
        maximise_button.bind('<Enter>', self.titlebar_button_enter)
        maximise_button.bind('<Leave>', self.titlebar_button_leave)
        exit_button = Button(title_bar, bg=self.rgb(32, 34, 37), activebackground=self.rgb(43, 46, 50), highlightthickness=0, bd=0, image=self.images_idle['exit'], name='exit', command=self.close_window)
        exit_button.place(x=title_bar.winfo_width() - 28, y=0, width=28, height=25)
        exit_button.bind('<Enter>', self.titlebar_button_enter)
        exit_button.bind('<Leave>', self.titlebar_button_leave)

        #----------------Received Message Area----------------#
        chatroom = Frame(self.root, bg=self.rgb(54, 57, 63))
        chatroom.place(x=0, y=25, width=screen_width - 240, height=self.root.winfo_height() - 25)
        chatroom.update()

        self.chatroom_inner = Text(chatroom, bg=self.rgb(54, 57, 63), fg='white', state='disabled')
        self.chatroom_inner.place(x=16, y=16, width=chatroom.winfo_width() - 32, height=chatroom.winfo_height() - 108)
        self.chatroom_inner.update()

        #----------------Members Panel----------------#
        self.members = Frame(self.root, bg=self.rgb(47, 49, 54))
        self.members.place(x=screen_width - 240, y=25, width=240, height=self.root.winfo_height() - 25)
        self.members.update()
        members_label = Label(self.members, text='Members Online', bg=self.rgb(47, 49, 54), fg='white', font=('Comic Sans', 14))
        members_label.place(x=10, y=10, width=self.members.winfo_width() - 20, height=25)

        #----------------User Input----------------#
        text_box_color = self.rgb(64, 68, 75)
        text_box_container = Frame(chatroom, bg=text_box_color)
        text_box_container.place(x=16, y=chatroom.winfo_height() - 68, width=chatroom.winfo_width() - 32, height=44)
        text_box_container.update()
        self.text_box_entry = Entry(text_box_container, bg=text_box_color, fg='white', insertbackground='white')
        self.text_box_entry.place(x=10, y=10, width=text_box_container.winfo_width() - 20, height=25)
        self.text_box_entry.bind('<Return>', self.send_message)
        self.text_box_entry.focus_set()

    def process_inbound_messages(self, inbound_message_queue):
        if inbound_message_queue:
            for message in inbound_message_queue:
                self.chatroom_inner.config(state='normal')
                self.chatroom_inner.insert(END, message + '\n')
                print(message)
                self.chatroom_inner.config(state='disabled')
                self.chatroom_inner.see(END)
                self.chatroom_inner.update()
            inbound_message_queue.clear()

    def process_online_user_queue(self, online_users_queue, online_users):
        if online_users_queue:
            for user_label in online_users:
                user_label.destroy()
            online_users.clear()
            for i in range(len(online_users_queue)):
                name = online_users_queue[i]
                if name == None:
                    name = 'None'
                online_user = Label(self.members, text=name, bg=self.rgb(47, 49, 54), fg='white', font=('Consolas', 12))
                online_user.place(x=10, y=(30 * i) + 50, width=self.members.winfo_width() - 20, height=30)
                online_user.bind('<Enter>', self.user_label_enter)
                online_user.bind('<Leave>', self.user_label_leave)
                online_users.append(online_user)
            online_users_queue.clear()

    def send_message(self, event):
        message = self.text_box_entry.get()
        self.outbound_message_queue.append(message)
        self.text_box_entry.delete(0, END)

    def user_label_enter(self, event):
        event.widget['background'] = self.rgb(52, 55, 60)

    def user_label_leave(self, event):
        event.widget['background'] = self.rgb(47, 49, 54)

    def titlebar_button_enter(self, event):
        name = str(event.widget)
        name = name.split('.')[-1]
        event.widget['image'] = self.images_hover[name]

    def titlebar_button_leave(self, event):
        name = str(event.widget)
        name = name.split('.')[-1]
        event.widget['image'] = self.images_idle[name]

    def toggle_window_visibility(self, event):
        if event.type == EventType.Map:
            self.root.deiconify()
        else:
            self.root.withdraw()

    def rgb(self, r, g, b):
        color = (r, g, b)
        return "#%02x%02x%02x" % color

    def minimise(self):
        self.root.withdraw()

    def maximise(self):
        pass

    def close_window(self):
        self.window.quit()
        self.window.destroy()
        sys.exit()

    def get_taskbar_height(self):
        monitor_info = GetMonitorInfo(MonitorFromPoint((0,0)))
        monitor_area = monitor_info.get("Monitor")
        work_area = monitor_info.get("Work")
        taskbar_height = monitor_area[3] - work_area[3]
        return taskbar_height

    def get_outbound_messages(self):
        return self.outbound_message_queue

    def update(self, inbound_message_queue, online_users_queue, online_users):
        self.process_inbound_messages(inbound_message_queue)
        self.process_online_user_queue(online_users_queue, online_users)
        self.window.update_idletasks()
        self.window.update()
