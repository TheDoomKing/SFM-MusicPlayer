﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MusicPlayer.Properties;

namespace MusicPlayer.Classes
{
    public class MyAppContext : ApplicationContext
    {
        public static  NotifyIcon TrayIcon;
        MainForm mainForm;

        public MyAppContext()
        {
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
            mainForm = new MainForm();
            TrayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
                ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("Show", Show),
                new MenuItem("Exit", Exit)
            }),
                Visible = true
            };
            TrayIcon.Click += new System.EventHandler(NotifyIcon_Click);
            mainForm.Show();
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            TrayIcon.Visible = false;
        }

        void Exit(object sender, EventArgs e)
        {
            //TrayIcon.Visible = false;
            Application.Exit();
        }

        void Show(object sender, EventArgs e)
        {
            mainForm.Show();
        }

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            var eventArgs = e as MouseEventArgs;
            switch (eventArgs.Button)
            {
                case MouseButtons.Left:
                    mainForm.Show();
                    break;
            }
        }
    }
}