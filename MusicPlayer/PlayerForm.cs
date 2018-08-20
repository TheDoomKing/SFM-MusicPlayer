﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;
using MusicPlayer.Classes;
using System.IO;
using System.Drawing.Text;
using Mp3Lib;
using TagLib;
using System.Runtime.InteropServices;

namespace MusicPlayer
{
    enum PlayState { OneSong, MultipleSongs, CSVPlaylist };
    public partial class MainForm : Form
    {
        PlayState State;

        WindowsMediaPlayer Choose;

        List<string> OriginalMusic;
        List<string> Music;
        List<string[]> OriginalSongDetails;
        List<string[]> SongDetails;

        string PlaylistPath;
        string GlobalPath;
        int CurrentSong;

        Timer t;

        private bool Shuffle;
        private bool Repeat;
        private bool IsPlaying;

        Image play      = Image.FromFile("..\\..\\Assets\\Play.png");
        Image pause     = Image.FromFile("..\\..\\Assets\\Pause.png");
        Image next      = Image.FromFile("..\\..\\Assets\\Next.png");
        Image previous  = Image.FromFile("..\\..\\Assets\\Previous.png");
        Image about     = Image.FromFile("..\\..\\Assets\\About.png");
        Image shuffle   = Image.FromFile("..\\..\\Assets\\Shuffle.png");
        Image shuffleON = Image.FromFile("..\\..\\Assets\\ShuffleON.png");
        Image repeat    = Image.FromFile("..\\..\\Assets\\Repeat.png");
        Image repeatON  = Image.FromFile("..\\..\\Assets\\RepeatON.png");
        Image settings  = Image.FromFile("..\\..\\Assets\\Settings.png");
        Image browse    = Image.FromFile("..\\..\\Assets\\Browse.png");
        Image Logo      = Image.FromFile("..\\..\\Assets\\AlbumArt.png");

        public MainForm()
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width, Screen.PrimaryScreen.WorkingArea.Height - this.Height);

            this.ChangeColors();
            this.PrepareButtons();
            this.CurrentSong = -1;
            
            Choose = new WindowsMediaPlayer();

            Choose.PlayStateChange += new WMPLib._WMPOCXEvents_PlayStateChangeEventHandler(Player_PlayStateChange);
            Choose.MediaError += new WMPLib._WMPOCXEvents_MediaErrorEventHandler(Player_MediaError);

            UseCustomFont("..\\..\\Assets\\segoeui.ttf", 18, this.lblTitle);
            UseCustomFont("..\\..\\Assets\\segoeui.ttf", 12, this.lblAlbum);
            UseCustomFont("..\\..\\Assets\\segoeui.ttf", 14, this.lblPerformer);

            this.picAlbum.Image = Logo;

            // TO CHANGE
            songProgressBar.Value = (int)Choose.controls.currentPosition;
            songProgressBar.ForeColor = Color.FromArgb(255, 0, 0);
            songProgressBar.BackColor = Color.FromArgb(80, 187, 0);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Shuffle = false;
            this.IsPlaying = false;
            this.Repeat = false;

            this.ShowInTaskbar = false;
        }

        void t_Tick(object sender, EventArgs e)
        {
            try
            {
                songProgressBar.Value = (int)this.Choose.controls.currentPosition;
            }
            catch (Exception ex)
            {
                
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {

            OpenFileDialog Open = new OpenFileDialog();
            Open.Filter = "MP3 Files|*.mp3|CSV Files|*.csv" /*+ "|All Files|*.*"*/;
            Open.InitialDirectory = MusicPlayer.SettingsForm.Path();
            Open.Multiselect = true;


            if (Open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Path.GetExtension(Open.FileName) == ".csv")
                {
                    State = PlayState.CSVPlaylist;

                    PlaylistPath = Open.FileName;
                    GlobalPath = Path.GetDirectoryName(PlaylistPath) + "\\";
                    OriginalSongDetails = Songs(PlaylistPath); //Take full path to .csv and return values
                    SongDetails = new List<string[]>(OriginalSongDetails);
                    OriginalMusic = new List<string>(Filenames()); //List of direct paths to each song
                    Music = new List<string>(OriginalMusic);


                    CurrentSong = 0;
                    string firstSong = Music[CurrentSong]; //Artist - Album.mp3


                    Choose.URL = firstSong;
                    SetDetails(CurrentSong);
                    Choose.controls.stop();

                    lblDuration.Text = Choose.currentMedia.durationString;
                }
                else //.mp3
                {
                    if (Open.FileNames.Length == 1)
                    {
                        State = PlayState.OneSong;
                        SongDetails = null;

                        GlobalPath = Path.GetDirectoryName(Open.FileName) + "\\";
                        OriginalMusic = new List<string>(Open.FileNames);
                        Music = new List<string>(OriginalMusic);

                        CurrentSong = 0;
                        Choose.URL = Open.FileName;
                        SetDetails(Open.FileName);

                        Choose.controls.stop();

                        lblDuration.Text = Choose.currentMedia.durationString;
                    }
                    else
                    {
                        State = PlayState.MultipleSongs;
                        SongDetails = null;

                        GlobalPath = Path.GetDirectoryName(Open.FileName) + "\\";
                        OriginalMusic = new List<string>(Open.FileNames);
                        Music = new List<string>(OriginalMusic);

                        CurrentSong = 0;
                        Choose.URL = Open.FileNames[CurrentSong];
                        SetDetails(Open.FileNames[CurrentSong]);

                        Choose.controls.stop();

                        lblDuration.Text = Choose.currentMedia.durationString;
                    }
                }
            }
        }

        private void btnActivation_Click(object sender, EventArgs e)
        {
            if (IsPlaying == false)
            {
                IsPlaying = true;
                btnActivation.Image = pause;
                Choose.controls.play();
            }
            else
            {
                IsPlaying = false;
                btnActivation.Image = play;
                Choose.controls.pause();
            }
        }

        //Take full path to .csv and return array of song details
        List<string[]> Songs(string FullPath)
        {
            List<string[]> songs = CSV.Import(FullPath, ',');
            return songs;
        }

        List<string> Filenames()
        {
            List<string> vs = new List<string>();
            foreach (var item in SongDetails)
            {
                vs.Add(GlobalPath + item[0] + " - " + item[1] + ".mp3");
            }
            return vs;
        }

        void SetDetails(int index)
        {
            if (State == PlayState.MultipleSongs)
            {
                SetDetails(Music.ElementAt(CurrentSong));
                return;
            }

            string performer = SongDetails.ElementAt(index)[0];
            string songName = SongDetails.ElementAt(index)[1];
            string album = SongDetails.ElementAt(index)[2];
            string fileName = Path.GetFileName(Music.ElementAt(index));

            string newTitle;
            string newAlbum;
            string newPerformer;
            string full = Music.ElementAt(index);
            Mp3File mp3 = new Mp3File(@full);
            try
            {
                try
                {
                    //this.picAlbum.Image = mp3.TagHandler.Picture;
                    /*Bitmap bitmap = new Bitmap(mp3.TagHandler.Picture, this.picAlbum.Size);
                    this.picAlbum.Image = bitmap;*/

                    TagLib.File file = TagLib.File.Create(@full);
                    MemoryStream ms = new MemoryStream(file.Tag.Pictures[0].Data.Data);
                    System.Drawing.Image image = System.Drawing.Image.FromStream(ms);
                    this.picAlbum.Image = new Bitmap(image, this.picAlbum.Size);
                }
                catch (Exception x)
                {
                    this.picAlbum.Image = Logo;
                }

                newTitle = mp3.TagHandler.Title;
                newAlbum = mp3.TagHandler.Album;
                newPerformer = mp3.TagHandler.Artist;

                if (newTitle == "")
                    throw new Exception("Title is empty");
                else
                    this.lblTitle.Text = newTitle;

                if (newPerformer == "")
                    throw new Exception("Performer is empty");
                else
                    this.lblPerformer.Text = newPerformer;

                if (newAlbum == "")
                    throw new Exception("Album is empty");
                else
                    this.lblAlbum.Text = newAlbum;
            }
            catch (Exception e)
            {
                this.lblTitle.Text = songName;
                this.lblPerformer.Text = performer;
                this.lblAlbum.Text = album;
            }
        }

        void SetDetails(string path)
        {
            string title, album, performer;

            try
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                int minus = fileName.LastIndexOf("-");

                title = fileName.Substring(minus + 2);
                performer = fileName.Substring(0, minus - 1);
                album = Path.GetFileName(path);
            }
            catch (Exception e)
            {
                performer = album = "";
                title = Path.GetFileName(@path);
            }


            string newTitle;
            string newAlbum;
            string newPerformer;

            Mp3File mp3 = new Mp3File(@path);
            try
            {
                try
                {
                    //this.picAlbum.Image = mp3.TagHandler.Picture;
                    /*Bitmap bitmap = new Bitmap(mp3.TagHandler.Picture, this.picAlbum.Size);
                    this.picAlbum.Image = bitmap;*/

                    TagLib.File file = TagLib.File.Create(@path);
                    MemoryStream ms = new MemoryStream(file.Tag.Pictures[0].Data.Data);
                    System.Drawing.Image image = System.Drawing.Image.FromStream(ms);
                    this.picAlbum.Image = new Bitmap(image, this.picAlbum.Size);
                }
                catch (Exception x)
                {
                    this.picAlbum.Image = Logo;
                }

                newTitle = mp3.TagHandler.Title;
                newAlbum = mp3.TagHandler.Album;
                newPerformer = mp3.TagHandler.Artist;

                if (newTitle == "")
                    throw new Exception("Title is empty");
                else
                    this.lblTitle.Text = newTitle;

                if (newPerformer == "")
                    throw new Exception("Performer is empty");
                else
                    this.lblPerformer.Text = newPerformer;

                if (newAlbum == "")
                    throw new Exception("Album is empty");
                else
                    this.lblAlbum.Text = newAlbum;

                this.lblTitle.Text = newTitle;
                this.lblPerformer.Text = newPerformer;
                this.lblAlbum.Text = newAlbum;
            }
            catch (Exception e)
            {
                newTitle = title;
                newAlbum = album;
                newPerformer = performer;
                
                this.lblTitle.Text = title;
                this.lblPerformer.Text = performer;
                this.lblAlbum.Text = album;
            }
        }

        void UseCustomFont(string name, int size, Label label)
        {
            PrivateFontCollection modernFont = new PrivateFontCollection();
            modernFont.AddFontFile(name);
            label.Font = new Font(modernFont.Families[0], size);
        }

        bool wasCalled = false; // Media stopped gets called twice
        private void Player_PlayStateChange(int NewState)
        {
            if ((WMPLib.WMPPlayState)NewState == WMPLib.WMPPlayState.wmppsStopped)
            {
                IsPlaying = false;
                if (wasCalled == true)
                {
                    wasCalled = false;
                    if (State == PlayState.OneSong)
                    {
                        if (Repeat)
                            Choose.controls.play();
                        else
                        {
                            IsPlaying = false; //SUBJECT TO REMOVE
                            return;
                        }
                    }
                    else
                    {
                        Choose.controls.play();  //NOT SURE WHY I ADDED IT
                        IsPlaying = true;
                        btnActivation.Image = pause;
                        return;
                    }
                }
                else
                {
                    wasCalled = true;
                    NextSong();
                }
            }
            else if ((WMPLib.WMPPlayState)NewState == WMPLib.WMPPlayState.wmppsPaused)
            {
                IsPlaying = false;
                btnActivation.Image = play;
            }
            else if ((WMPLib.WMPPlayState)NewState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                IsPlaying = true;

                t = new Timer();
                t.Interval = 100;
                t.Tick += new EventHandler(t_Tick);

                btnActivation.Image = pause;
            }
            else if ((WMPLib.WMPPlayState)NewState == WMPLib.WMPPlayState.wmppsMediaEnded)
            {
                btnActivation.Image = play;
                IsPlaying = false;
            }

            if (Choose.openState == WMPLib.WMPOpenState.wmposMediaOpen)
            {
                songProgressBar.Maximum = (int)Choose.currentMedia.duration;
                lblDuration.Text = Choose.currentMedia.durationString;
                t.Start();
            }
        }

        private void Player_MediaError(object pMediaObject)
        {
            MessageBox.Show("Cannot play media file.");
            //this.Close();
        }

        void NextSong()
        {
            if (State != PlayState.OneSong)
            {
                if (Music.Count - 1 > CurrentSong) // Not final song
                {
                    Choose.URL = Music.ElementAt(CurrentSong + 1);
                    Choose.controls.play();
                    CurrentSong++;
                    SetDetails(CurrentSong);
                }
                else //if (Music.Count - 1 == CurrentSong) // Final song
                {
                    if (Repeat)
                    {
                        CurrentSong = -1;
                        Choose.URL = Music.ElementAt(CurrentSong + 1);
                        Choose.controls.play();
                        CurrentSong++;
                        SetDetails(CurrentSong);
                    }
                    else
                    {
                        //IsPlaying = false;
                    }
                }
            }
            else
            {
                if (Repeat)
                    Choose.controls.play();
                else
                    IsPlaying = false; 
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            NextSong();
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if (CurrentSong > 0)
            {
                Choose.URL = Music.ElementAt(CurrentSong - 1);
                if (IsPlaying == false)
                {
                    IsPlaying = true;
                    btnActivation.Image = pause;
                }
                CurrentSong--;
                SetDetails(CurrentSong);
            }
        }

        private void btnShuffle_Click(object sender, EventArgs e)
        {
            if (State != PlayState.OneSong)
            {
                if (Shuffle) //Turned on
                {
                    Music = new List<string>(OriginalMusic);
                    if (OriginalSongDetails != null)
                        SongDetails = new List<string[]>(OriginalSongDetails);
                    this.btnShuffle.Image = shuffle;
                    Shuffle = false;
                }
                else
                {
                    ShuffleList(Music, SongDetails);
                    this.btnShuffle.Image = shuffleON;
                    Shuffle = true;
                }
            }
        }

        // Randomize the list
        private Random rng = new Random();
        public void ShuffleList<T>(List<T> list, List<string[]> list2 = null)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;

                if (list2 != null)
                {
                    string[] vs = list2[k];
                    list2[k] = list2[n];
                    list2[n] = vs;
                }
            }
        }
        // End Randomize List

        private void btnRepeat_Click(object sender, EventArgs e)
        {
            if (Repeat) //Turned on
            {
                this.btnRepeat.Image = repeat;
                Repeat = false;
            }
            else
            {
                this.btnRepeat.Image = repeatON;
                Repeat = true;
            }
        }

        private void CSVCreate(object sender, EventArgs e)
        {
            CSV.CreateCSV();
        }

        private void songProgressBar_MouseClick(object sender, MouseEventArgs e)
        {
            if (Choose.playState == WMPLib.WMPPlayState.wmppsPlaying)
            {
                float x = (float)e.X / songProgressBar.Width;
                Choose.controls.currentPosition = x * Choose.currentMedia.duration;
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.Show();
        }

        private void btnInfo_Click(object sender, EventArgs e)
        {
            AboutForm about = new AboutForm();
            about.Show();
        }
    }
}