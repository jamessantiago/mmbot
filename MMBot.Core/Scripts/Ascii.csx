﻿
// Ported from https://github.com/rbwestmoreland/Jabbot/blob/master/Jabbot.Sprockets.Community/AsciiSprocket.cs

var robot = Require<Robot>();

private const string Url = "http://asciime.heroku.com/generate_ascii?s={0}";


robot.Respond(@"(ascii)( me)? (.*)", msg =>
{
    var query = msg.Match[3];

    msg.Http(String.Format(Url, query))
        .Get((err, res) =>  {
    
        try
        {
            res.Content.ReadAsStringAsync().ContinueWith(readTask => msg.Send(readTask.Result));
        }
        catch (Exception)
        {
            msg.Send("erm....issues, move along");
        }
    });
});

robot.AddHelp("mmbot ascii me <query> - Returns ASCII art of the query text.");
