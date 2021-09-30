using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.MiddlewareUtils.LocalDocker
{
    //info from appconfig 
    public class LocalDockerSettings
    {
        public static bool UseLocalHPC { get; set;} = false;
        public static string Ip { get; set;}//{ get => @"127.0.0.1"; }
        public static int Port { get; set; }// = 2375; //default exposed port at docker
        public static int ContainerExposedPort { get; set; } //= 49005; //default exposed port at container
        public static string ContainerName { get; set; } //= "localhpc";
        public static string ImageConfigurationDir { get; set; }
    }
}