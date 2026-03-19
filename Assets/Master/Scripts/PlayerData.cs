using System.Threading.Tasks;
using UnityEngine;

namespace Master.Scripts
{
    public class PlayerData
    {
        /*
         Last Level;
         Last Scene;
         Last Postition;
         Last Active Task;
         Task Status;
         */

        public int lastLevel;
        public int lastScene;
        public Vector3 lastPosition;
        public Task lastActiveTask;
        public TaskStatus lastActiveStatus;
    }
}