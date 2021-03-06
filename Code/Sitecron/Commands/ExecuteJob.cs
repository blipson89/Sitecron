﻿using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecron.SitecronSettings;
using System;

namespace Sitecron.Commands
{
    public class ExecuteJob : Command
    {
        public override void Execute(CommandContext context)
        {
            Assert.IsNotNull(context, "context");
            Assert.IsNotNull(context.Parameters["id"], "id");

            string contextDbName = Settings.GetSetting(SitecronConstants.SettingsNames.SiteCronContextDB); 
            Database contextDb = Factory.GetDatabase(contextDbName);

            Item scriptItem = contextDb.GetItem(new ID(context.Parameters["id"]));
            if (scriptItem != null && scriptItem.TemplateID == SitecronConstants.Templates.SitecronJobTemplateID)
            {
                string newItemName = ItemUtil.ProposeValidItemName(string.Concat("Execute Now ", scriptItem.Name, DateTime.Now.ToString(" yyyyMMddHHmmss")));

                Item autoFolderItem = contextDb.GetItem(new ID(SitecronConstants.ItemIds.AutoFolderID));
                if (autoFolderItem != null)
                {
                    Item newScriptItem = scriptItem.CopyTo(autoFolderItem, newItemName);

                    double addExecutionSeconds = 20;
                    if (!Double.TryParse(Settings.GetSetting(SitecronConstants.SettingsNames.SiteCronExecuteNowSeconds), out addExecutionSeconds))
                        addExecutionSeconds = 20;

                    using (new EditContext(newScriptItem, Sitecore.SecurityModel.SecurityCheck.Disable))
                    {
                        DateTime executeTime = DateTime.Now.AddSeconds(addExecutionSeconds);
                        newScriptItem[SitecronConstants.FieldNames.CronExpression] = string.Format("{0} {1} {2} 1/1 * ? * ", executeTime.ToString("ss"), executeTime.ToString("mm"), executeTime.ToString("HH"));
                        newScriptItem[SitecronConstants.FieldNames.ArchiveAfterExecution] = "1";
                        newScriptItem[SitecronConstants.FieldNames.Disable] = "0";
                    }
                }
            }
        }

        public override CommandState QueryState(CommandContext context)
        {
            if (context.Items.Length != 1)
                return CommandState.Hidden;

            Item currentItem = context.Items[0];
            if (currentItem != null && currentItem.Template.ID.Equals(SitecronConstants.Templates.SitecronJobTemplateID))
            {
                return CommandState.Enabled;
            }

            return CommandState.Hidden;
        }
    }
}