using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RealtimeToDo.Hubs
{
    public class ToDoHub : Hub
    {
        private static List<string> tasks = new List<string>();

        public async Task SendEditTask(int taskIndex, string newTaskText)
        {
            if (taskIndex >= 0 && taskIndex < tasks.Count)
            {
                tasks[taskIndex] = newTaskText;
                await Clients.All.SendAsync("ReceiveEditTask", taskIndex, newTaskText);
            }
        }

        public async Task SendNewTask(string taskText)
        {
            tasks.Add(taskText);
            await Clients.All.SendAsync("ReceiveNewTask", taskText);
        }

        public async Task SendDeleteTask(int taskIndex)
        {
            if (taskIndex >= 0 && taskIndex < tasks.Count)
            {
                tasks.RemoveAt(taskIndex);
                await Clients.All.SendAsync("ReceiveDeleteTask", taskIndex);
            }
        }

        public async Task SendMoveTask(int oldIndex, int newIndex)
        {
            if (oldIndex >= 0 && oldIndex < tasks.Count &&
                newIndex >= 0 && newIndex < tasks.Count)
            {
                string item = tasks[oldIndex];
                tasks.RemoveAt(oldIndex);
                tasks.Insert(newIndex, item);

                await Clients.Others.SendAsync("ReceiveMoveTask", oldIndex, newIndex);
            }
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("ReceiveCurrentTasks", tasks);
            await base.OnConnectedAsync();
        }
    }
}