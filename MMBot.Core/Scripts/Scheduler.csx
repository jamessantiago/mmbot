﻿/**
* <description>
*     Creates timers to execute mmbot commands on a schedule
* </description>
*
* <configuration>
*
* </configuration>
*
* <commands>
*     mmbot repeat every &lt;time&gt; &lt;command&gt;- Repeat an mmbot command on a schedule (e.g. 4m, 1s, 24h).;
*     mmbot schedule for &lt;time> &lt;command&gt; - Schedule an mmbot command.;
*     mmbot list schedule- Lists all scheduled jobs.;
*     mmbot stop job &lt;job|all&gt;- kills a scheduled job, all for all jobs.;
* </commands>
* 
* <notes>
*     Storing messages in the brain doesn't seem to be working at the moment  (maybe the class is too complex?)
* </notes>
* 
* <author>
*     jamessantiago
* </author>
*/


var robot = Require<Robot>();

public class MessageJob
{
	public Message Message;
	public string Schedule;
	public System.Timers.Timer JobTimer;
}

public class SavedJob
{
	public Message Message;
	public string Schedule;
	public double Interval;
}

List<MessageJob> scheduledJobs = new List<MessageJob>();
var previouslySavedJobs = robot.Brain.Get<List<SavedJob>>("Schedule").Result ?? new List<SavedJob>();

foreach (var savedJob in previouslySavedJobs)
{
	var t = new System.Timers.Timer();
	t.Elapsed += (sender, e) => { HandleMessageTimerElapsed(savedJob.Message, robot); };
	t.Interval = savedJob.Interval;
	t.Enabled = true;

	scheduledJobs.Add(new MessageJob()
	{
		Message = savedJob.Message,
		Schedule = savedJob.Schedule,
		JobTimer = t
	});
}

robot.Respond(@"(repeat every (\d*)(\w) )(.*)", msg =>
{
    var repeatTxt = msg.Match[1];
	var timeValue = int.Parse(msg.Match[2]);
	var timeType = msg.Match[3];
	var cmdText = robot.Name + " " + msg.Match[4];	

	Message m = new TextMessage(msg.Message.User, cmdText, null);

	TimeSpan interval = GetTimeSpanFromRelative(timeValue, timeType); 
	if (interval.TotalMilliseconds == 0)
	{
		msg.Send("Could not understand time given");
		return;
	}

	var t = new System.Timers.Timer();
	t.Elapsed += (sender, e) => { HandleMessageTimerElapsed(m, robot); };
	t.Interval = interval.TotalMilliseconds;
	t.Enabled = true;

	scheduledJobs.Add(new MessageJob(){
		Message = m,
		Schedule = "on repeat at an interval of " + timeValue.ToString() + timeType,
		JobTimer = t
	});
	var savedJobs = robot.Brain.Get<List<SavedJob>>("Schedule").Result ?? new List<SavedJob>();
	savedJobs.Add(new SavedJob(){
		Message = m,
		Schedule = "on repeat at an interval of " + timeValue.ToString() + timeType,
		Interval = interval.TotalMilliseconds
	});
	robot.Brain.Set<List<SavedJob>>("Schedule", savedJobs);
	msg.Send(string.Format("Ok, I'll run \"{0}\" on repeat at an interval of {1}", cmdText, timeValue.ToString() + timeType));

});

robot.Respond(@"(schedule for (\d*)(\w) )(.*)", msg =>
{
    var repeatTxt = msg.Match[1];
	var timeValue = int.Parse(msg.Match[2]);
	var timeType = msg.Match[3];
	var cmdText = robot.Name + " " + msg.Match[4];	

	Message m = new TextMessage(msg.Message.User, cmdText, null);

	TimeSpan interval = GetTimeSpanFromRelative(timeValue, timeType); 
	if (interval.TotalMilliseconds == 0)
	{
		msg.Send("Could not understand time given");
		return;
	}

	var t = new System.Timers.Timer();
	t.Elapsed += (sender, e) => { HandleMessageTimerElapsed(m, robot); };
	t.Interval = interval.TotalMilliseconds;
	t.AutoReset = false;
	t.Enabled = true;

	scheduledJobs.Add(new MessageJob(){
		Message = m,
		Schedule = "at " + DateTime.Now.AddMilliseconds(interval.TotalMilliseconds).ToString(),
		JobTimer = t
	});
	var savedJobs = robot.Brain.Get<List<SavedJob>>("Schedule").Result ?? new List<SavedJob>();
	savedJobs.Add(new SavedJob()
	{
		Message = m,
		Schedule = "on repeat at an interval of " + timeValue.ToString() + timeType,
		Interval = interval.TotalMilliseconds
	});
	robot.Brain.Set<List<SavedJob>>("Schedule", savedJobs);
	msg.Send(string.Format("Ok, I'll run \"{0}\" in the next {1}", cmdText, timeValue.ToString() + timeType));

});

robot.Respond(@"list schedule(s)?(d jobs)?", msg => 
{
	foreach (var job in scheduledJobs)
	{
		msg.Send(string.Format("\"{0}\" scheduled to execute {1}", ((TextMessage)job.Message).Text, job.Schedule));
	}
});

robot.Respond(@"(stop|kill)( job)? (.*)", msg =>
{
	var savedJobs = robot.Brain.Get<List<SavedJob>>("Schedule").Result ?? new List<SavedJob>();
	if (msg.Match[3].ToLower() == "all")
	{
		foreach (var j in scheduledJobs)
		{
			j.JobTimer.Enabled = false;
		}
		scheduledJobs.Clear();
		savedJobs.Clear();
		robot.Brain.Set<List<SavedJob>>("Schedule", savedJobs);
		msg.Send("Killed all jobs");
	}
	var job = scheduledJobs.Where(d => ((TextMessage)d.Message).Text.ToLower() == msg.Match[3].ToLower()).FirstOrDefault();
	if (job == null)
	{
		msg.Send("could not find that");
	}
	else
	{
		job.JobTimer.Enabled = false;
		savedJobs.Remove(savedJobs.Where(d => d.Message == job.Message).FirstOrDefault());
		scheduledJobs.Remove(job);
		robot.Brain.Set<List<SavedJob>>("Schedule", savedJobs);
		msg.Send("Killed the job");
	}
});

private static void HandleMessageTimerElapsed(Message m, Robot robot)
{
	robot.Receive(m);
}

private static TimeSpan GetTimeSpanFromRelative(int timeValue, string timeType)
{
	switch (timeType.ToLower())
	{
		case "d":
			return new TimeSpan(timeValue, 0, 0, 0, 0);
			break;
		case "h":
			return new TimeSpan(0, timeValue, 0, 0, 0);
			break;
		case "m":
			return new TimeSpan(0, 0, timeValue, 0, 0);
			break;
		case "s":
			return new TimeSpan(0, 0, 0, timeValue, 0);
			break;
		default:
			return new TimeSpan(0);
			break;
	}
}
