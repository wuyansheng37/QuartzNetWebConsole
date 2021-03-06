﻿Imports MiniMVC
Imports Quartz
Imports System.Web

Public Module Views
    Public Function Pager(pagination As PaginationInfo) As XElement
        Return _
            <div class="pagination">
                Showing <%= pagination.FirstItemIndex %> - <%= pagination.LastItemIndex %> of <%= pagination.TotalItemCount %>

                <%= If(pagination.HasPrevPage,
                    <a href=<%= pagination.PrevPageUrl %>><%= X.laquo %> Previous</a>,
                    <span class="disabledPage"><%= X.laquo %> Previous</span>) %>

                <%= From p In pagination.Pages
                    Select
                    If(p = pagination.CurrentPage,
                    <span class="currentPage"><%= p %></span>,
                    <a href=<%= pagination.PageUrlFor(p) %>><%= p %></a>) %>

                <%= If(pagination.HasNextPage,
                    <a href=<%= pagination.NextPageUrl %>><%= X.raquo %> Next</a>,
                    <span class="disabledPage"><%= X.raquo %> Next</span>) %>
            </div>
    End Function

    Public Function Log(ByVal logs As IEnumerable(Of LogEntry), ByVal pagination As PaginationInfo, ByVal thisUrl As String) As XElement
        Return _
            <html>
                <head>
                    <title>Quartz.Net Console - Log</title>
                    <%= Stylesheet %>
                    <link rel="alternate" type="application/rss+xml" title="RSS" href="log.ashx?rss=1"/>
                </head>
                <body>
                    <a href="index.ashx">Index</a>
                    <h1>Scheduler log</h1>
                    <table>
                        <tr>
                            <th>Date / Time</th>
                            <th>Description</th>
                        </tr>
                        <%= From e In logs
                            Select
                            <tr>
                                <td><%= e.Timestamp %></td>
                                <td><%= X.Raw(e.Description) %></td>
                            </tr>
                        %>
                    </table>
                    <br/>
                    <%= Pager(pagination) %>
                </body>
            </html>
    End Function

    Public Function LogRSS(ByVal thisUrl As String, ByVal logs As IEnumerable(Of LogEntry)) As XElement
        Return _
<rss version="2.0">
    <channel>
        <title>Quartz.NET log</title>
        <description>Quartz.NET log</description>
        <link><%= thisUrl %></link>
        <pubDate><%= DateTimeOffset.Now.ToString("R") %></pubDate>
        <%= From r In logs
            Select
            <item>
                <title><%= r.Timestamp %></title>
                <description><%= r.Description %></description>
                <pubDate><%= r.Timestamp.ToString("R") %></pubDate>
            </item>
        %>
    </channel>
</rss>
    End Function

    Public Function SchedulerStatus(ByVal scheduler As IScheduler, ByVal metadata As SchedulerMetaData) As XElement
        Return _
            <div class="group">
                <h2>Scheduler name: <%= scheduler.SchedulerName %></h2>
                <div style="float: left">
				    Job store: <%= metadata.JobStoreType %><br/>
				    Supports persistence: <%= YesNo(metadata.JobStoreSupportsPersistence) %><br/>
				    Number of jobs executed: <%= metadata.NumJobsExecuted %><br/>
				    Running since: <%= metadata.RunningSince %><br/>
				    Status: <%= If(scheduler.InStandbyMode, "stand-by", "running") %>
                    <br/>
                    <a href="log.ashx">View log</a>
                </div>
                <div style="float: right">
                    <%= SimpleForm("scheduler.ashx?method=Shutdown", "Shut down") %>
                    <%= If(scheduler.InStandbyMode,
                        SimpleForm("scheduler.ashx?method=Start", "Start"),
                        SimpleForm("scheduler.ashx?method=Standby", "Stand by")) %>
                    <%= SimpleForm("scheduler.ashx?method=PauseAll", "Pause all triggers") %>
                    <%= SimpleForm("scheduler.ashx?method=ResumeAll", "Resume all triggers") %>
                </div>
            </div>

    End Function

    Public Function SchedulerListeners(ByVal scheduler As IScheduler) As XElement
        Return _
            <div class="group">
                <h2>Scheduler listeners</h2>
                <table>
                    <tr>
                        <th>Type</th>
                    </tr>
                    <%= From l In scheduler.SchedulerListeners.Cast(Of Object)()
                        Select
                        <tr>
                            <td><%= l.GetType() %></td>
                        </tr>
                    %>
                </table>
            </div>
    End Function

    Public Function SchedulerCalendars(ByVal calendars As ICollection(Of KeyValuePair(Of String, String))) As XElement
        Return _
            <div class="group">
                <h2>Calendars</h2>
                <%= If(calendars.Count = 0,
                    <span>No calendars</span>,
                    <table>
                        <tr>
                            <th>Name</th>
                            <th>Description</th>
                        </tr>
                        <%= From cal In calendars
                            Select
                            <tr>
                                <td><%= cal.Key %></td>
                                <td><%= cal.Value %></td>
                            </tr>
                        %>
                    </table>) %>
            </div>

    End Function

    Public Function SchedulerEntityGroups(entity As String) As Func(Of IEnumerable(Of GroupWithStatus), XElement)
        Return Function(groups) _
                    <div class="group">
                        <h2><%= entity %> groups</h2>
                        <table>
                            <tr>
                                <th>Name</th>
                                <th>Status</th>
                                <th></th>
                            </tr>
                            <%= From group In groups
                                Select
                                <tr>
                                    <td>
                                        <a href=<%= entity & "Group.ashx?group=" & group.Name %>><%= group.Name %></a>
                                    </td>
                                    <td><%= If(group.Paused, "Paused", "Started") %></td>
                                    <td>
                                        <%= If(group.Paused,
                                            SimpleForm("scheduler.ashx?method=Resume" & entity & "Group&groupName=" + group.Name, "Resume"),
                                            SimpleForm("scheduler.ashx?method=Pause" & entity & "Group&groupName=" + group.Name, "Pause")) %>
                                    </td>
                                </tr>
                            %>
                        </table>
                    </div>
    End Function

    Public ReadOnly SchedulerJobGroups As Func(Of IEnumerable(Of GroupWithStatus), XElement) = SchedulerEntityGroups("Job")

    Public ReadOnly SchedulerTriggerGroups As Func(Of IEnumerable(Of GroupWithStatus), XElement) = SchedulerEntityGroups("Trigger")

    Public Function GlobalEntityListeners(entity As String) As Func(Of ICollection(Of KeyValuePair(Of String, Type)), XElement)
        Return Function(listeners) _
                <div class="group">
                    <h2>Global <%= entity %> listeners</h2>
                    <%= If(listeners.Count = 0,
                        <span>No <%= entity %> listeners</span>,
                        <table>
                            <tr>
                                <th>Name</th>
                                <th></th>
                            </tr>
                            <%= From listener In listeners
                                Select
                                <tr>
                                    <td><%= listener.Key %></td>
                                    <td>
                                        <%= SimpleForm("scheduler.ashx?method=RemoveGlobal" & entity & "Listener&name=" & listener.Key, "Delete") %>
                                    </td>
                                </tr>
                            %>
                        </table>) %>
                </div>
    End Function

    Public ReadOnly GlobalJobListeners As Func(Of ICollection(Of KeyValuePair(Of String, Type)), XElement) = GlobalEntityListeners("Job")

    Public ReadOnly GlobalTriggerListeners As Func(Of ICollection(Of KeyValuePair(Of String, Type)), XElement) = GlobalEntityListeners("Trigger")

    Public Function IndexPage(ByVal scheduler As IScheduler,
                              ByVal metadata As SchedulerMetaData,
                              ByVal triggerGroups As IEnumerable(Of GroupWithStatus),
                              ByVal jobGroups As IEnumerable(Of GroupWithStatus),
                              ByVal calendars As ICollection(Of KeyValuePair(Of String, String)),
                              ByVal jobListeners As ICollection(Of KeyValuePair(Of String, Type)),
                              ByVal triggerListeners As ICollection(Of KeyValuePair(Of String, Type))) As XElement
        Return _
        <html>
            <head>
                <title>Quartz.Net Console</title>
                <%= Stylesheet %>
            </head>
            <body>
                <%= SchedulerStatus(scheduler, metadata) %>
                <%= SchedulerListeners(scheduler) %>
                <%= SchedulerCalendars(calendars) %>
                <br style="clear:both"/>
                <%= SchedulerJobGroups(jobGroups) %>
                <%= SchedulerTriggerGroups(triggerGroups) %>
                <br style="clear:both"/>
                <%= GlobalJobListeners(jobListeners) %>
                <%= GlobalTriggerListeners(triggerListeners) %>
            </body>
        </html>
    End Function

    Public Function TriggerGroup(ByVal group As String, ByVal paused As Boolean, ByVal thisUrl As String, ByVal highlight As String, ByVal triggers As IEnumerable(Of TriggerWithState)) As XElement
        Dim schedulerOp = Function(method As String) "scheduler.ashx?method=" + method +
                              "&groupName=" + group +
                              "&next=" + HttpUtility.UrlEncode(thisUrl)
        Return _
        <html>
            <head>
                <title>Quartz.Net console - Trigger group <%= group %></title>
                <%= Stylesheet %>
            </head>
            <body>
                <a href="index.ashx">Index</a>
                <h1>Trigger group <%= group %></h1>
		        Status: <%= If(paused, "paused", "started") %>
                <%= If(paused,
                    SimpleForm(schedulerOp("ResumeTriggerGroup"), "Resume this trigger group"),
                    SimpleForm(schedulerOp("PauseTriggerGroup"), "Pause this trigger group")) %>
                <br style="clear:both"/>
                <h2>Triggers</h2>
                <%= TriggerTable(triggers, thisUrl, highlight) %>
            </body>
        </html>
    End Function

    Public Function JobGroup(ByVal group As String, ByVal paused As Boolean, ByVal highlight As String, ByVal thisUrl As String, ByVal jobs As IEnumerable(Of JobWithContext)) As XElement
        Dim schedulerOp = Function(method As String) "scheduler.ashx?method=" + method +
                              "&groupName=" + group +
                              "&next=" + HttpUtility.UrlEncode(thisUrl)
        Return _
            <html>
                <head>
                    <title>Quartz.Net console - Job group <%= group %></title>
                    <%= Stylesheet %>
                </head>
                <body>
                    <a href="index.ashx">Index</a>
                    <h1>Job group <%= group %></h1>
		            Status: <%= If(paused, "paused", "started") %>
                    <%= If(paused,
                        SimpleForm(schedulerOp("ResumeJobGroup"), "Resume this job group"),
                        SimpleForm(schedulerOp("PauseJobGroup"), "Pause this job group")) %>
                    <br style="clear:both"/>
                    <h2>Jobs</h2>
                    <table>
                        <tr>
                            <th>Name</th>
                            <th>Description</th>
                            <th>Type</th>
                            <th>Durable</th>
                            <th>Stateful</th>
                            <th>Volatile</th>
                            <th>Requests recovery</th>
                            <th>Running since</th>
                            <th>Listeners</th>
                            <th></th>
                        </tr>
                        <%= From j In jobs
                            Let op = Function(method As String) "scheduler.ashx?method=" + method +
                            "&jobName=" + j.Job.Name +
                            "&groupName=" + j.Job.Group +
                            "&next=" + HttpUtility.UrlEncode(thisUrl)
                            Select
                            <tr id=<%= j.Job.FullName %>
                                class=<%= If(highlight = j.Job.FullName, "highlight", "") %>>
                                <td><%= j.Job.Name %></td>
                                <td><%= j.Job.Description %></td>
                                <td><%= j.Job.JobType %></td>
                                <td><%= YesNo(j.Job.Durable) %></td>
                                <td><%= YesNo(j.Job.Stateful) %></td>
                                <td><%= YesNo(j.Job.Volatile) %></td>
                                <td><%= YesNo(j.Job.RequestsRecovery) %></td>
                                <td><%= j.JobContext.FireTimeUtc %></td>
                                <td><%= j.Job.JobListenerNames.Length %></td>
                                <td>
                                    <a href=<%= "triggersByJob.ashx?group=" + j.Job.Group + "&job=" + j.Job.Name %>>Triggers</a>
                                    <%= SimpleForm(op("DeleteJob"), "Delete") %>
                                    <%= SimpleForm(op("PauseJob"), "Pause") %>
                                    <%= SimpleForm(op("ResumeJob"), "Resume") %>
                                    <%= SimpleForm(op("TriggerJob"), "Trigger") %>
                                    <%= SimpleForm(op("TriggerJobWithVolatileTrigger"), "Trigger volatile") %>
                                    <%= If(j.Interruptible, SimpleForm(op("Interrupt"), "Interrupt"), Nothing) %>
                                </td>
                            </tr>
                        %>
                    </table>
                </body>
            </html>
    End Function

    Public Function TriggersByJob(ByVal model As TriggersByJobModel) As XElement
        Return _
<html>
    <head>
        <title>Quartz.Net console - Triggers for job <%= model.Group %>.<%= model.Job %></title>
        <%= Stylesheet %>
    </head>
    <body>
        <a href="index.ashx">Index</a>
        <h1>Triggers for job <%= model.Group %>.<%= model.Job %></h1>
        <br style="clear:both"/>
        <h2>Triggers</h2>
        <%= TriggerTable(model.Triggers, model.ThisUrl, model.Highlight) %>
    </body>
</html>
    End Function

    Public Function TriggerTable(ByVal triggers As IEnumerable(Of TriggerWithState), ByVal thisUrl As String, ByVal highlight As String) As XElement
        Return _
            <table>
                <tr>
                    <th>Name</th>
                    <th>Description</th>
                    <th>Priority</th>
                    <th>Job group</th>
                    <th>Job name</th>
                    <th>Start time UTC</th>
                    <th>End time UTC</th>
                    <th>Final fire time UTC</th>
                    <th>Next fire time UTC</th>
                    <th>Repeat count</th>
                    <th>Repeat interval</th>
                    <th>Times triggered</th>
                    <th>Cron</th>
                    <th>Calendar</th>
                    <th>State</th>
                    <th></th>
                </tr>
                <%= From tr In triggers
                    Let trigger = tr.Trigger
                    Let high = highlight = trigger.FullName
                    Let simpleTrigger = TryCast(trigger, SimpleTrigger)
                    Let cronTrigger = TryCast(trigger, CronTrigger)
                    Let op = Function(method As String) "scheduler.ashx?method=" & method &
                    "&triggerName=" + trigger.Name +
                    "&groupName=" + trigger.Group +
                    "&next=" + HttpUtility.UrlEncode(thisUrl)
                    Select
                    <tr id=<%= trigger.FullName %>
                        class=<%= If(highlight = trigger.FullName, highlight, "") %>>
                        <td><%= trigger.Name %></td>
                        <td><%= trigger.Description %></td>
                        <td><%= trigger.Priority %></td>
                        <td>
                            <a href=<%= "jobGroup.ashx?group=" + trigger.JobGroup %>><%= trigger.JobGroup %></a>
                        </td>
                        <td>
                            <a href=<%= "jobGroup.ashx?group=" + trigger.JobGroup +
                                        "&highlight=" + trigger.JobGroup + "." + trigger.JobName +
                                        "#" + trigger.JobGroup + "." + trigger.JobName %>>
                                <%= trigger.JobGroup %>
                            </a>
                        </td>
                        <td><%= trigger.StartTimeUtc %></td>
                        <td><%= trigger.EndTimeUtc %></td>
                        <td><%= trigger.FinalFireTimeUtc %></td>
                        <td><%= trigger.GetNextFireTimeUtc() %></td>
                        <td><%= If(simpleTrigger IsNot Nothing, simpleTrigger.RepeatCount.ToString, "") %></td>
                        <td><%= If(simpleTrigger IsNot Nothing, simpleTrigger.RepeatInterval.ToString, "") %></td>
                        <td><%= If(simpleTrigger IsNot Nothing, simpleTrigger.TimesTriggered.ToString, "") %></td>
                        <td><%= If(cronTrigger IsNot Nothing, X.SpacesToNbsp(cronTrigger.CronExpressionString), "") %></td>
                        <td><%= trigger.CalendarName %></td>
                        <td><%= tr.State %></td>
                        <td>
                            <%= If(tr.IsPaused,
                                SimpleForm(op("ResumeTrigger"), "Resume"),
                                SimpleForm(op("PauseTrigger"), "Pause")) %>

                            <%= SimpleForm(op("UnschedulerJob"), "Delete") %>
                        </td>
                    </tr> %>
            </table>
    End Function
End Module
