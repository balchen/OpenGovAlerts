﻿using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace OpenGovAlerts.Services
{
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            return true;
        }
    }
}
