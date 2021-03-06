﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Activities;
using System.Activities.Statements;
using System.Xml.Linq;
using Orleans.Activities.Hosting;
using Orleans.Activities.Notification;
using Orleans.Activities.Persistence;

namespace Orleans.Activities.Extensions
{
    public static class TimerExtensionExtensions
    {
        public static TimerExtension GetTimerExtension(this ActivityContext context)
        {
            TimerExtension timerExtension = context.GetExtension<TimerExtension>();
            if (timerExtension == null)
                throw new ValidationException(nameof(TimerExtension) + " is not found.");
            return timerExtension;
        }
    }

    /// <summary>
    /// This extension is always created by the workflow instance to prevent original workflow activities to create a default DurableTimerExtension.
    /// This way the TimerExtension functions are always implemented by DurableReminderExtension and not by the default DurableTimerExtension.
    /// <para>See <see cref="ReminderTable"/> comments about the Orleans reminder/persistence logic incompatible original DurableTimerExtension.</para>
    /// </summary>
    public class DurableReminderExtension : TimerExtension, INotificationParticipant, IPersistenceIOParticipant
    {
        protected ReminderTable reminderTable;

        public DurableReminderExtension(IActivityContext instance)
        {
            reminderTable = new ReminderTable(instance);
        }

        public bool IsReactivationReminder(string reminderName) => ReminderTable.IsReactivationReminder(reminderName);

        public Bookmark GetBookmark(string reminderName) => reminderTable.GetBookmark(reminderName);

        public void UnregisterReminder(string reminderName)
        {
            reminderTable.UnregisterReminder(reminderName);
        }

        #region TimerExtension members

        protected override void OnRegisterTimer(TimeSpan timeout, Bookmark bookmark)
        {
            reminderTable.RegisterOrUpdateReminder(bookmark, timeout);
        }

        protected override void OnCancelTimer(Bookmark bookmark)
        {
            reminderTable.UnregisterReminder(bookmark);
        }

        #endregion

        #region INotificationParticipant members

        // TODO handle timeout
        public Task OnPausedAsync(TimeSpan timeout) => reminderTable.OnPausedAsync();

        #endregion

        #region IPersistenceIOParticipant members

        // TODO handle timeout
        public Task OnSaveAsync(IDictionary<XName, object> readWriteValues, IDictionary<XName, object> writeOnlyValues, TimeSpan timeout) =>
            reminderTable.OnSavingAsync();

        // TODO handle timeout
        public Task OnSavedAsync(TimeSpan timeout) =>
            reminderTable.OnSavedAsync();

        // TODO handle timeout
        public Task OnLoadAsync(IDictionary<XName, object> readWriteValues, TimeSpan timeout) =>
            reminderTable.LoadAsync(readWriteValues);

        public void CollectValues(out IDictionary<XName, object> readWriteValues, out IDictionary<XName, object> writeOnlyValues)
        {
            reminderTable.CollectValues(out readWriteValues, out writeOnlyValues);
        }

        public IDictionary<XName, object> MapValues(IDictionary<XName, object> readWriteValues, IDictionary<XName, object> writeOnlyValues) =>
            null;

        public void PublishValues(IDictionary<XName, object> readWriteValues)
        { }

        // TODO handle Abort
        public void Abort()
        { }

        #endregion
    }
}
