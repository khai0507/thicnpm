using DocTask.Core.Dtos.Reminders;
using DocTask.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocTask.Service.Mappers
{
    public static class ReminderMapper
    {
        public static Reminder ToRemider (RemiderRequest request)
        {
            return new Reminder {
                Taskid = request.Taskid,
                Periodid = request.Periodid,
                Title = request.Title,
                Message = request.Message,
                Triggertime = request.Triggertime,
                Isauto = request.Isauto,
                Createdby = request.Createdby,
                Createdat = request.Createdat ?? DateTime.UtcNow,
                Isnotified = request.Isnotified ?? false,
                Notifiedat = request.Notifiedat,
                Notificationid = request.Notificationid,
                UserId = request.UserId
            };
        }

        public static ReminderDto FromRemider (Reminder reminder)
        {
            return new ReminderDto {
                Reminderid = reminder.Reminderid,
                Taskid = reminder.Taskid,
                Periodid = reminder.Periodid,
                Title = reminder.Title,
                Message = reminder.Message,
                Triggertime = reminder.Triggertime,
                Isauto = reminder.Isauto,
                Createdby = reminder.Createdby,
                Createdat = reminder.Createdat,
                Isnotified = reminder.Isnotified,
                Notifiedat = reminder.Notifiedat,
                Notificationid = reminder.Notificationid,
                UserId = reminder.UserId
            };
        }

    }
}
