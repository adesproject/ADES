using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace AdesUnrealController
{
    class Actor
    {

        public static String VEHICLE_CAMERA_1 = "RobotCamera";

        private static string vehicleName = "car";
        private static string vehicleClass = "USARBot.Sedan";

        private static double FRONTSTEER_DELTA = Math.PI / 36d;
        private static double FRONTSTEER_NORMALIZE_COEF = 0.3d;
        private static int SPEED_INCREMENT = 1;
        private static int SPEED_DECREMENT = 4;
        private static int MAX_SPEED = 100;

        private int speed = 0;
        private double frontSteer = 0;

        private Messaging msgSys;

        public Actor(Messaging msgSys)
        {
            this.msgSys = msgSys;
            msgSys.UnrealMsg += new Messaging.UnrealMsgHandler(this.UnrealCallback); 

        }

        public int getSpeed()
        {
            return speed;
        }

        public void increaseSpeed()
        {
            if (speed + SPEED_INCREMENT > MAX_SPEED) return;
            msgSys.sendCommand(Messaging.CMD_DRIVE + " {Speed " + (speed + SPEED_INCREMENT) + "}");
            speed += SPEED_INCREMENT;
        }

        public void decreaseSpeed()
        {
            if (speed - SPEED_DECREMENT < -MAX_SPEED/3) return;
            msgSys.sendCommand(Messaging.CMD_DRIVE + " {Speed " + (speed - SPEED_DECREMENT) + "}");
            speed -= SPEED_DECREMENT;
        }

        public void steerLeft()
        {
            //UnrealMsg retval = msgSys.getMsgResponse(Messaging.MSG_STA_GV);
            //double current;
            frontSteer += FRONTSTEER_DELTA;
            if (frontSteer > Math.PI / 3d)
                frontSteer = Math.PI / 3d;

            //if (retval != null && retval["FrontSteer"] != null && Double.TryParse(retval["FrontSteer"].ToString(), out current))
            //    frontSteer = current;
            msgSys.sendCommand(Messaging.CMD_DRIVE + " {FrontSteer " + (frontSteer + FRONTSTEER_DELTA) + "}");
            //frontSteer += FRONTSTEER_DELTA;
        }

        public void steerRight()
        {
            //UnrealMsg retval = msgSys.getMsgResponse(Messaging.MSG_STA_GV);
            //double current;
            frontSteer -= FRONTSTEER_DELTA;
            if (frontSteer < -Math.PI / 4d)
                frontSteer = -Math.PI / 4d;

            //if (retval != null && retval["FrontSteer"] != null && Double.TryParse(retval["FrontSteer"].ToString(), out current))
            //    frontSteer = current;
            msgSys.sendCommand(Messaging.CMD_DRIVE + " {FrontSteer " + (frontSteer - FRONTSTEER_DELTA) + "}");
            //frontSteer -= FRONTSTEER_DELTA;
        }

        public void speedNormalize()
        {
            if (speed > 0)
                if (speed > SPEED_INCREMENT)
                    speed -= SPEED_INCREMENT;
                else
                    speed = 0;
            else
                if (speed < -SPEED_INCREMENT)
                    speed += SPEED_INCREMENT;
                else
                    speed = 0;
            msgSys.sendCommand(Messaging.CMD_DRIVE + " {Speed " + speed + "}");
        }

        public void steerNormalize()
        {
            //UnrealMsg retval = msgSys.getMsgResponse(Messaging.MSG_STA_GV);
            //double current;
            /*if (retval != null && retval["FrontSteer"] != null && Double.TryParse(retval["FrontSteer"].ToString(), out current))
                frontSteer = current;*/
            if (frontSteer > 0)
                if (frontSteer > FRONTSTEER_DELTA / FRONTSTEER_NORMALIZE_COEF)
                    frontSteer -= FRONTSTEER_DELTA / FRONTSTEER_NORMALIZE_COEF;
                else
                    frontSteer = 0d;
            else
                if (frontSteer < -FRONTSTEER_DELTA / FRONTSTEER_NORMALIZE_COEF)
                    frontSteer += FRONTSTEER_DELTA / FRONTSTEER_NORMALIZE_COEF;
                else
                    frontSteer = 0d;
            msgSys.sendCommand(Messaging.CMD_DRIVE + " {FrontSteer " + frontSteer + "}");
        }

        public void initVehicle()
        {
            UnrealMsg ret = msgSys.sendAndReceive(Messaging.CMD_GETSTARTPOSES, Messaging.MSG_NFO_SP);
            Console.WriteLine(ret);
            ret = msgSys.sendAndReceive(Messaging.CMD_INIT + " {ClassName " + vehicleClass + "} {Location " + ret["PlayerStart"] + "} {Name " + vehicleName + "} {Rotation 0,0," + Math.Round(Math.PI * 0, 2) + "}", Messaging.MSG_STA_GV);
            Console.WriteLine(ret);
            msgSys.sendCommand(Messaging.CMD_SET_CAMERA + " {Robot " + vehicleName + "} {Name " + VEHICLE_CAMERA_1 + "} {Client " + AdesDlg.getLocalIP() + "}");
            msgSys.sendCommand(Messaging.CMD_GETGEO);
        }

        public void setCamera(String camera)
        {
            if (camera == null)
                msgSys.sendCommand(Messaging.CMD_SET_CAMERA + " {Client " + AdesDlg.getLocalIP() + "}");
            else
                msgSys.sendCommand(Messaging.CMD_SET_CAMERA + " {Robot " + vehicleName + "} {Name " + camera + "} {Client " + AdesDlg.getLocalIP() + "}");
        }

        public void UnrealCallback(object sender, UnrealMsgArgs ua)
        {
            //Console.WriteLine(msgNum);
            if (ua.getMsgNum() % 5 == 0)
            {
                steerNormalize();
                speedNormalize();
            }
        }
    }
}
