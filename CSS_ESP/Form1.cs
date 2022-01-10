using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Memory;
using ezOverLay;
using System.Threading;

namespace CSS_ESP
{
    public partial class Form1 : Form
    {
        ez ez = new ez();
        Mem m = new Mem();
        int MyTeam;
        Graphics g;
        Pen TeamPen = new Pen(Color.Blue, 3);
        Pen AnotherTeamPen = new Pen(Color.Red, 3);

        List<Entity> Entities = new List<Entity>();

        #region MEM
        string VIEWMATRIX = "engine.dll+0x005AAAA4,0x2D4";
        string LOCALPLAYER = "client.dll+0x004C88E8";
        int ENTITYBASE = 0x004D5AE4;
        string HP = ",0x94";
        string XYZ = ",0x260";
        string TEAM = ",0x9C";
        string DORMANT = ",0x17C";
        string GAME = "client.dll+";
        #endregion
  

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            int PID = m.GetProcIdFromName("hl2");

            if (PID > 0)
            {
                m.OpenProcess(PID);
                ez.SetInvi(this);
                ez.DoStuff("Counter-Strike Source", this);
                Thread ESPThread = new Thread(ESP) { IsBackground = true};
                ESPThread.Start();
            }
        }

        void ESP()
        {
            while (true)
            {
                Entities.Clear();
                MyTeam = m.ReadInt(LOCALPLAYER+TEAM);

                for (int i=0; i < 21; i++)
                {
                    string CurrentStr = GAME + "0x" + (ENTITYBASE + i * 0x10).ToString("X");
                    int Dormant = m.ReadInt(CurrentStr + DORMANT);
                    int Health = m.ReadInt(CurrentStr + HP);
                    if (Dormant != 0 || Health < 1 || Health > 100) continue;

                    int Team = m.ReadInt(CurrentStr + TEAM);
                    byte[] buffer = new byte[12];
                    var bytes = m.ReadBytes(CurrentStr+ XYZ, (long)buffer.Length);
                    var Entity = new Entity
                    {
                        xyz = new Vector3
                        {
                            x = BitConverter.ToSingle(bytes, 0),
                            y = BitConverter.ToSingle(bytes, 4),
                            z = BitConverter.ToSingle(bytes, 8)
                        },
                        health = Health,
                        dormant = Dormant,
                        team = Team
                    };

                    Entity.bottom = WorldToScreen(IntoTheMatrix(), Entity.xyz, Width, Height, false);
                    Entity.top = WorldToScreen(IntoTheMatrix(), Entity.xyz, Width, Height, true);

                    Entities.Add(Entity);




                }
                panel1.Refresh();
                Thread.Sleep(10);
            }
        }

        Pen EnemyHp(int hp)
        {
            if (hp > 80)
                return new Pen(Color.FromArgb(16, 255, 0), 3);
           else if (hp > 60)
                return new Pen(Color.FromArgb(64, 204, 0), 3);
            else if(hp > 40)
                return new Pen(Color.FromArgb(112, 153, 0), 3);
            else if(hp > 20)
                return new Pen(Color.FromArgb(159, 102, 0), 3);
            else if(hp > 10)
                return new Pen(Color.FromArgb(207, 51, 0), 3);
            else if(hp > 1)
                return new Pen(Color.FromArgb(255, 0, 0), 3);
            return new Pen(Color.Black, 3);

        }
    
        Point WorldToScreen(ViewMatrix viewMatrix, Vector3 vec, int width, int height, bool head)
        {
            if (head)
            {
                vec.z += 58;
            }

            var twoD = new Point();
            float screenW = (viewMatrix.m41 * vec.x) + (viewMatrix.m42 * vec.y) + (viewMatrix.m43 * vec.z) + viewMatrix.m44;
            if (screenW > 0.001f)
            {
                float screenX = (viewMatrix.m11 * vec.x) + (viewMatrix.m12 * vec.y) + (viewMatrix.m13 * vec.z) + viewMatrix.m14;
                float screenY = (viewMatrix.m21 * vec.x) + (viewMatrix.m22 * vec.y) + (viewMatrix.m23 * vec.z) + viewMatrix.m24;
                float camX = width / 2f;
                float camY = height / 2f;
                float X = camX + (camX * screenX / screenW);
                float Y = camY - (camY * screenY / screenW);

                twoD.X = (int)X;
                twoD.Y = (int)Y;
                return twoD;
            }
            else
            {
                return new Point(-99, -99);
            }



        }

        ViewMatrix IntoTheMatrix()
        {
            var matrix = new ViewMatrix();
            byte[] buffer = new byte[16 * 4];
            var bytes = m.ReadBytes(VIEWMATRIX, (long)buffer.Length);

            matrix.m11 = BitConverter.ToSingle(bytes, (0 * 4));
            matrix.m12 = BitConverter.ToSingle(bytes, (1 * 4));
            matrix.m13 = BitConverter.ToSingle(bytes, (2 * 4));
            matrix.m14 = BitConverter.ToSingle(bytes, (3 * 4));

            matrix.m21 = BitConverter.ToSingle(bytes, (4 * 4));
            matrix.m22 = BitConverter.ToSingle(bytes, (5 * 4));
            matrix.m23 = BitConverter.ToSingle(bytes, (6 * 4));
            matrix.m24 = BitConverter.ToSingle(bytes, (7 * 4));

            matrix.m31 = BitConverter.ToSingle(bytes, (8 * 4));
            matrix.m32 = BitConverter.ToSingle(bytes, (9 * 4));
            matrix.m33 = BitConverter.ToSingle(bytes, (10 * 4));
            matrix.m34 = BitConverter.ToSingle(bytes, (11 * 4));

            matrix.m41 = BitConverter.ToSingle(bytes, (12 * 4));
            matrix.m42 = BitConverter.ToSingle(bytes, (13 * 4));
            matrix.m43 = BitConverter.ToSingle(bytes, (14 * 4));
            matrix.m44 = BitConverter.ToSingle(bytes, (15 * 4));

            return matrix;


        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            g = e.Graphics;
            foreach (var ent in Entities)
            {

                try
                {
                    if (ent.team == MyTeam && ent.health > 1 && ent.health < 101)
                    {
                        if (ent.bottom.X > 0 && ent.bottom.Y < Height && ent.bottom.X < Width && ent.bottom.Y > 0)
                        {
                            g.DrawLine(TeamPen, Width / 2, Height, ent.bottom.X, ent.bottom.Y);
                            g.DrawRectangle(TeamPen, ent.rect());
                       }
                    }
                    else 
                    {
                        if (ent.bottom.X > 0 && ent.bottom.Y < Height && ent.bottom.X < Width && ent.bottom.Y > 0)
                       {
                            g.DrawLine(AnotherTeamPen, Width / 2, Height, ent.bottom.X, ent.bottom.Y);
                            g.DrawRectangle(EnemyHp(ent.health), ent.rect());
                       }
                    }

                }
                catch
                {

                }

            }
        }
    }
}
