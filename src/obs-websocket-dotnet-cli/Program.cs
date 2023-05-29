using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualBasic;
using OBSWebsocketDotNet;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using OBSWebsocketDotNet.Communication;

namespace ObsWebSocketDotNetCli;

public static class Program
{
    private static OBSWebsocket _obs = new OBSWebsocket();
    private static bool isConnected;
    private static string disconnectInfo = "";

    public static void Main()
    {
        string[] args = Environment.GetCommandLineArgs();
        string password = "";
        string server = "ws://127.0.0.1:4455";

        string profile = "";
        string scene = "";
        string hidesource = "";
        string showsource = "";
        string togglesource = "";
        string toggleaudio = "";
        string mute = "";
        string unmute = "";
        string fadeopacity = "";
        string slidesetting = "";
        string slideasync = "";
        string setvolume = "";
        bool stopstream;
        bool startstream;
        bool startrecording;
        bool stoprecording;
        string sendjson;
        string command = "";
        double setdelay;

        string errormessage = "";

        if (args.Length == 1)
        {
            PrintUsage();
            System.Environment.Exit(0);
        }

        bool isInitialized = false;
        bool skipfirst = false;
        int argsindex = 0;

        _obs.Connected += Connect;
        _obs.Disconnected += Disconnect;

        foreach (string arg in args)
        {
            argsindex += 1;
            if (skipfirst == false)
            {
                skipfirst = true;
                continue;
            }

            // Clear variables
            profile = "";
            scene = "";
            command = "";
            sendjson = "";
            hidesource = "";
            showsource = "";
            togglesource = "";
            toggleaudio = "";
            mute = "";
            unmute = "";
            setvolume = "";
            fadeopacity = "";
            slidesetting = "";
            slideasync = "";
            stopstream = false;
            startstream = false;
            startrecording = false;
            stoprecording = false;

            if (arg == "?" | arg == "/?" | arg == "-?" | arg == "help" | arg == "/help" | arg == "-help")
            {
                PrintUsage();
                System.Environment.Exit(0);
            }

            if (arg.StartsWith("/server=", StringComparison.OrdinalIgnoreCase))
            {
                server = "ws://" + arg.Replace("/server=", "", StringComparison.OrdinalIgnoreCase);
                continue; // get credentials first before trying to connect!
            }
            if (arg.StartsWith("/password=", StringComparison.OrdinalIgnoreCase))
            {
                password = arg.Replace("/password=", "", StringComparison.OrdinalIgnoreCase);
                continue; // get credentials first before trying to connect!
            }

            if (arg.StartsWith("/setdelay=", StringComparison.OrdinalIgnoreCase))
            {
                string tmp = arg.Replace("/setdelay=", "", StringComparison.OrdinalIgnoreCase);
                tmp = tmp.Replace(",", ".", StringComparison.OrdinalIgnoreCase);
                if (!Information.IsNumeric(tmp))
                {
                    Console.WriteLine("Error: setdelay is not numeric");
                    continue;
                }
                else
                {
                    setdelay = double.Parse(tmp, System.Globalization.CultureInfo.InvariantCulture);
                    continue;
                }
            }

            if (arg.StartsWith("/delay=", StringComparison.OrdinalIgnoreCase))
            {
                string tmp = arg.Replace("/delay=", "", StringComparison.OrdinalIgnoreCase);
                tmp = tmp.Replace(",", ".", StringComparison.OrdinalIgnoreCase);
                if (double.TryParse(tmp, out double _delay))
                {
                    Thread.Sleep((int)(_delay * 1000));
                    continue;
                }
                else
                {
                    Console.WriteLine("Error: delay is not numeric");
                    continue;
                }
            }

            if (arg.StartsWith("/profile=", StringComparison.OrdinalIgnoreCase))
                profile = arg.Replace("/profile=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/scene=", StringComparison.OrdinalIgnoreCase))
                scene = arg.Replace("/scene=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/command=", StringComparison.OrdinalIgnoreCase))
                command = arg.Replace("/command=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/sendjson=", StringComparison.OrdinalIgnoreCase))
                sendjson = arg.Replace("/sendjson=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/hidesource=", StringComparison.OrdinalIgnoreCase))
                hidesource = arg.Replace("/hidesource=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/showsource=", StringComparison.OrdinalIgnoreCase))
                showsource = arg.Replace("/showsource=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/togglesource=", StringComparison.OrdinalIgnoreCase))
                togglesource = arg.Replace("/togglesource=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/toggleaudio=", StringComparison.OrdinalIgnoreCase))
                toggleaudio = arg.Replace("/toggleaudio=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/mute=", StringComparison.OrdinalIgnoreCase))
                mute = arg.Replace("/mute=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/unmute=", StringComparison.OrdinalIgnoreCase))
                unmute = arg.Replace("/unmute=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/setvolume=", StringComparison.OrdinalIgnoreCase))
                setvolume = arg.Replace("/setvolume=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/fadeopacity=", StringComparison.OrdinalIgnoreCase))
                fadeopacity = arg.Replace("/fadeopacity=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/slidesetting=", StringComparison.OrdinalIgnoreCase))
                slidesetting = arg.Replace("/slidesetting=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.StartsWith("/slideasync=", StringComparison.OrdinalIgnoreCase))
                slideasync = arg.Replace("/slideasync=", "", StringComparison.OrdinalIgnoreCase);
            if (arg.Equals("/startstream", StringComparison.OrdinalIgnoreCase))
                startstream = true;
            if (arg.Equals("/stopstream", StringComparison.OrdinalIgnoreCase))
                stopstream = true;
            if (arg.Equals("/startrecording", StringComparison.OrdinalIgnoreCase))
                startrecording = true;
            if (arg.Equals("/stoprecording", StringComparison.OrdinalIgnoreCase))
                stoprecording = true;

            try
            {
                if (isInitialized == false)
                {
                    isInitialized = true;
                    _obs.WSTimeout = new TimeSpan(0, 0, 0, 3);
                    _obs.ConnectAsync(server, password);
                    int i = 0;
                    while (!isConnected)
                    {
                        Thread waitThread = new Thread(() =>
                        {
                            Thread.Sleep(10);
                        });
                        waitThread.Start();
                        waitThread.Join();
                        i += 1;
                        if (i > 300)
                        {
                            Console.Write("Error: can't connect to OBS websocket plugin!");
                            System.Environment.Exit(0);
                        }
                        if (!string.IsNullOrEmpty(disconnectInfo))
                        {
                            Console.Write("Error: " + disconnectInfo);
                            System.Environment.Exit(0);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(profile))
                {
                    JObject fields = new JObject();
                    fields.Add("profileName", profile);
                    _obs.SendRequest("SetCurrentProfile", fields);
                }

                if (!string.IsNullOrEmpty(scene))
                {
                    JObject fields = new JObject();
                    fields.Add("sceneName", scene);
                    _obs.SendRequest("SetCurrentProgramScene", fields);
                }

                // sendjson
                if (!string.IsNullOrEmpty(sendjson))
                {
                    JObject json = new JObject();
                    if (!sendjson.Contains("=", StringComparison.OrdinalIgnoreCase))
                    {
                        errormessage = "sendjson missing \"=\" after command";
                        break;
                    }
                    string[] tmp = new[] { "", "" };
                    try
                    {
                        tmp[0] = sendjson.Substring(0, sendjson.IndexOf("=", StringComparison.OrdinalIgnoreCase));
                        tmp[1] = sendjson.Substring(sendjson.IndexOf("=", StringComparison.OrdinalIgnoreCase) + 1);
                        tmp[1] = tmp[1].Replace("'", "\"", StringComparison.OrdinalIgnoreCase);
                        json = JObject.Parse(tmp[1]);
                        Console.WriteLine(_obs.SendRequest(tmp[0], json));
                    }
                    catch (Exception ex)
                    {
                        errormessage = "sendjson error:" + Constants.vbCrLf + ex.Message;
                    }
                }

                if (!string.IsNullOrEmpty(command))
                {
                    command = command.Replace("'", "\"", StringComparison.OrdinalIgnoreCase);

                    try
                    {
                        if (command.Contains(",", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] tmp = command.Split(",");

                            JObject fields = new JObject();
                            for (var a = 1; a <= tmp.Length - 1; a++)
                            {
                                string[] tmpsplit = SplitWhilePreservingQuotedValues(tmp[a], '=', true);
                                if (tmpsplit.Length < 2)
                                    Console.WriteLine("Error with command \"" + command + "\": " + "Missing a = in Name=Type");

                                if (tmpsplit.Length > 2)
                                {
                                    JObject subfield = new JObject();
                                    subfield.Add(ConvertToType(tmpsplit[1]), ConvertToType(tmpsplit[2]));
                                    fields.Add(ConvertToType(tmpsplit[0]), subfield);
                                }
                                else
                                {
                                    fields.Add(ConvertToType(tmpsplit[0]), ConvertToType(tmpsplit[1]));
                                }
                            }

                            Console.WriteLine(_obs.SendRequest(tmp[0], fields));
                        }
                        else
                            Console.WriteLine(_obs.SendRequest(command));
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine("Error with command \"" + command + "\": " + ex.Message.ToString());
                    }
                }
                if (hidesource != "")
                {
                    if (hidesource.Contains("/"))
                    {
                        string[] tmp = hidesource.Split("/");

                        // scene/source
                        if (tmp.Count() == 2)
                        {
                            JObject fields = new JObject();
                            fields.Add("sceneName", tmp[0]);
                            fields.Add("sceneItemId", GetSceneItemId(tmp[0], tmp[1]));
                            fields.Add("sceneItemEnabled", false);
                            _obs.SendRequest("SetSceneItemEnabled", fields);
                        }
                    }
                    else
                    {
                        var CurrentScene = GetCurrentProgramScene();
                        JObject fields = new JObject();
                        fields.Add("sceneName", CurrentScene);
                        fields.Add("sceneItemId", GetSceneItemId(CurrentScene, hidesource));
                        fields.Add("sceneItemEnabled", false);
                        _obs.SendRequest("SetSceneItemEnabled", fields);
                    }
                }
                if (showsource != "")
                {
                    if (showsource.Contains("/"))
                    {
                        string[] tmp = showsource.Split("/");

                        // scene/source
                        if (tmp.Count() == 2)
                        {
                            JObject fields = new JObject();
                            fields.Add("sceneName", tmp[0]);
                            fields.Add("sceneItemId", GetSceneItemId(tmp[0], tmp[1]));
                            fields.Add("sceneItemEnabled", true);
                            _obs.SendRequest("SetSceneItemEnabled", fields);
                        }
                    }
                    else
                    {
                        var CurrentScene = GetCurrentProgramScene();
                        JObject fields = new JObject();
                        fields.Add("sceneName", CurrentScene);
                        fields.Add("sceneItemId", GetSceneItemId(CurrentScene, showsource));
                        fields.Add("sceneItemEnabled", true);
                        _obs.SendRequest("SetSceneItemEnabled", fields);
                    }
                }
                if (togglesource != "")
                {
                    if (togglesource.Contains("/"))
                    {
                        string[] tmp = togglesource.Split("/");

                        // scene/source
                        if (tmp.Count() == 2)
                            OBSToggleSource(tmp[1], tmp[0]);
                    }
                    else
                        OBSToggleSource(togglesource);
                }
                if (toggleaudio != "")
                {
                    JObject fields = new JObject();
                    fields.Add("inputName", toggleaudio);
                    _obs.SendRequest("ToggleInputMute", fields);
                }
                if (mute != "")
                {
                    JObject fields = new JObject();
                    fields.Add("inputName", mute);
                    fields.Add("inputMuted", true);
                    _obs.SendRequest("SetInputMute", fields);
                }
                if (unmute != "")
                {
                    JObject fields = new JObject();
                    fields.Add("inputName", unmute);
                    fields.Add("inputMuted", false);
                    _obs.SendRequest("SetInputMute", fields);
                }

                if (fadeopacity != "")
                {
                    // source,filtername,startopacity,endopacity,[fadedelay],[fadestep]
                    string[] tmp = fadeopacity.Split(",");
                    if (tmp.Count() < 4)
                        throw new Exception("/fadeopacity is missing required parameters!");
                    if (!IsNumericOrAsterix(tmp[2]) | !IsNumericOrAsterix(tmp[3]))
                        throw new Exception("Opacity start or end value is not nummeric (0-100)!");
                    if (tmp.Count() == 4)
                        DoSlideSetting(tmp[0], tmp[1], "opacity", tmp[2], tmp[3]);
                    else if (tmp.Count() == 5)
                    {
                        if (!Information.IsNumeric(tmp[4]))
                            throw new Exception("Delay value is not nummeric (0-x)!");
                        DoSlideSetting(tmp[0], tmp[1], "opacity", tmp[2], tmp[3], tmp[4]);
                    }
                    else if (tmp.Count() == 6)
                    {
                        if (!Information.IsNumeric(tmp[4]))
                            throw new Exception("Delay value is not nummeric (0-x)!");
                        if (!Information.IsNumeric(tmp[5]))
                            throw new Exception("Fadestep value is not nummeric (1-x)!");
                        DoSlideSetting(tmp[0], tmp[1], "opacity", tmp[2], tmp[3], tmp[4], tmp[5]);
                    }
                }

                if (slidesetting != "")
                {
                    // source,filtername,settingname,startvalue,endvalue,[slidedelay],[slidestep]
                    string[] tmp = slidesetting.Split(",");
                    if (tmp.Count() < 5)
                        throw new Exception("/slideSetting is missing required parameters!");
                    if (!IsNumericOrAsterix(tmp[3]) | !IsNumericOrAsterix(tmp[4]))
                        throw new Exception("Slide start or end value is not nummeric (0-100)!");
                    if (tmp.Count() == 5)
                        DoSlideSetting(tmp[0], tmp[1], tmp[2], tmp[3], tmp[4]);
                    else if (tmp.Count() == 6)
                    {
                        if (!Information.IsNumeric(tmp[5]))
                            throw new Exception("Delay value is not nummeric (0-x)!");
                        DoSlideSetting(tmp[0], tmp[1], tmp[2], tmp[3], tmp[4], tmp[5]);
                    }
                    else if (tmp.Count() == 7)
                    {
                        if (!Information.IsNumeric(tmp[5]))
                            throw new Exception("Delay value is not nummeric (0-x)!");
                        if (!Information.IsNumeric(tmp[6]))
                            throw new Exception("Slide step value is not nummeric (1-x)!");
                        DoSlideSetting(tmp[0], tmp[1], tmp[2], tmp[3], tmp[4], tmp[5], tmp[6]);
                    }
                }

                if (slideasync != "")
                {
                    // source,filtername,settingname,startvalue,endvalue,[slidedelay],[slidestep]
                    string[] tmp = slideasync.Split(",");
                    if (tmp.Count() < 5)
                        throw new Exception("/slideSetting is missing required parameters!");
                    if (!IsNumericOrAsterix(tmp[3]) | !IsNumericOrAsterix(tmp[4]))
                        throw new Exception("Slide start or end value is not nummeric (0-100)!");
                    if (tmp.Count() == 5)
                    {
                        AsyncSlideSettings ExecuteTask = new AsyncSlideSettings(server, password, tmp[0], tmp[1], tmp[2], tmp[3], tmp[4]);
                        System.Threading.Thread t;
                        t = new System.Threading.Thread(ExecuteTask.StartSlide());
                        t.IsBackground = true;
                        t.Start();
                    }
                    else if (tmp.Count() == 6)
                    {
                        if (!Information.IsNumeric(tmp[5]))
                            throw new Exception("Delay value is not nummeric (0-x)!");
                        AsyncSlideSettings ExecuteTask = new AsyncSlideSettings(server, password, tmp[0], tmp[1], tmp[2], tmp[3], tmp[4], tmp[5]);
                        System.Threading.Thread t;
                        t = new System.Threading.Thread(ExecuteTask.StartSlide());
                        t.IsBackground = true;
                        t.Start();
                    }
                    else if (tmp.Count() == 7)
                    {
                        if (!Information.IsNumeric(tmp[5]))
                            throw new Exception("Delay value is not nummeric (0-x)!");
                        if (!Information.IsNumeric(tmp[6]))
                            throw new Exception("Slide step value is not nummeric (1-x)!");
                        AsyncSlideSettings ExecuteTask = new AsyncSlideSettings(server, password, tmp[0], tmp[1], tmp[2], tmp[3], tmp[4], tmp[5], tmp[6]);
                        System.Threading.Thread t;
                        t = new System.Threading.Thread(ExecuteTask.StartSlide());
                        t.IsBackground = true;
                        t.Start();
                    }
                }

                if (setvolume != "")
                {
                    // source,volume,[delay],[steps]
                    string[] tmp = setvolume.Split(",");
                    if (!Information.IsNumeric(tmp[1]))
                        throw new Exception("Volume value is not nummeric (0-100)!");
                    if (tmp.Count() == 2)
                        OBSSetVolume(tmp[0], tmp[1]);
                    else if (tmp.Count() == 3)
                    {
                        if (!Information.IsNumeric(tmp[2]))
                            throw new Exception("Delay value is not nummeric (5-1000)!");
                        OBSSetVolume(tmp[0], tmp[1], tmp[2]);
                    }
                    else if (tmp.Count() == 4)
                    {
                        if (!Information.IsNumeric(tmp[2]))
                            throw new Exception("Delay value is not nummeric (5-1000)!");
                        if (!Information.IsNumeric(tmp[3]))
                            throw new Exception("Step value is not nummeric (1-99)!");
                        OBSSetVolume(tmp[0], tmp[1], tmp[2], tmp[3]);
                    }
                }
                if (startstream == true)
                    // _obs.SendRequest("StartStreaming")
                    _obs.SendRequest("StartStream");
                if (stopstream == true)
                    // _obs.SendRequest("StopStreaming")
                    _obs.SendRequest("StopStream");
                if (startrecording == true)
                    // _obs.SendRequest("StartRecording")
                    _obs.SendRequest("StartRecord");
                if (stoprecording == true)
                    // _obs.SendRequest("StopRecording")
                    _obs.SendRequest("StopRecord");

                if (setdelay > 0 & argsindex < args.Count() & argsindex > 1)
                    System.Threading.Thread.Sleep(setdelay * 1000);
            }
            catch (Exception ex)
            {
                errormessage = ex.Message.ToString();
            }
        }
        try
        {
            _obs.Disconnect();
        }
        catch (Exception ex)
        {
        }

        // Console.SetOut(myout)
        if (errormessage == "")
            Console.Write("Ok");
        else
            Console.Write("Error: " + errormessage);
    }

    private static void Connect(object sender, EventArgs e)
    {
        Debug.WriteLine("Connection established!");
        isConnected = true;
    }

    private static void Disconnect(object sender, ObsDisconnectionInfo e)
    {
        Debug.WriteLine("Connection terminated: " + e.DisconnectReason);
        disconnectInfo = e.DisconnectReason;
    }

    private static bool IsNumericOrAsterix(string value)
    {
        if (value == "*")
            return true;

        if (!Information.IsNumeric(value))
            return false;

        return true;
    }

    private static JToken ConvertToType(string text)
    {
        if (Information.IsNumeric(text))
        {
            if (text.Contains("."))
                return double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
            else
            {
                if (System.Convert.ToInt64(text) > int.MaxValue | System.Convert.ToInt64(text) < int.MinValue)
                    return System.Convert.ToInt64(text);
                return System.Convert.ToInt32(text);
            }
        }
        else if (text.ToUpper() == "TRUE" | text.ToUpper() == "FALSE")
            return System.Convert.ToBoolean(text);
        else
            return text;
    }

    private static int GetSceneItemId(string sceneName, string sourceName)
    {
        JObject fields = new JObject();
        fields.Add("sceneName", ConvertToType(sceneName));
        fields.Add("sourceName", ConvertToType(sourceName));

        JObject response = _obs.SendRequest("GetSceneItemId", fields);

        return response.GetValue("sceneItemId");
    }

    private static string GetCurrentProgramScene()
    {
        JObject response = _obs.SendRequest("GetCurrentProgramScene");

        return response.GetValue("currentProgramSceneName");
    }

    private static void OBSToggleSource(string source, string sceneName = "")
    {
        if (sceneName == "")
            sceneName = GetCurrentProgramScene();

        JObject fields = new JObject();
        fields.Add("sceneName", sceneName);
        fields.Add("sceneItemId", GetSceneItemId(sceneName, source));
        JObject sceneItemEnabled = _obs.SendRequest("GetSceneItemEnabled", fields);
        fields.Add("sceneItemEnabled", !bool.Parse(sceneItemEnabled.GetValue("sceneItemEnabled")));
        _obs.SendRequest("SetSceneItemEnabled", fields);
    }

    private static void DoSlideSetting(string source, string filtername, string settingname, string fadestart, string fadeend, int delay = 0, string fadestep = "1")
    {
        if (delay < 5)
            delay = 5;
        if (delay > 1000)
            delay = 1000;

        if (fadestart == "*" | fadeend == "*")
        {
            JObject tmpfield = new JObject();
            tmpfield.Add("sourceName", source);
            tmpfield.Add("filterName", filtername);
            JObject result = _obs.SendRequest("GetSourceFilter", tmpfield);
            if (fadestart == "*")
            {
                JObject tmp = result.GetValue("filterSettings");
                fadestart = tmp.GetValue(settingname);
            }
            else if (fadeend == "*")
            {
                JObject tmp = result.GetValue("filterSettings");
                fadeend = tmp.GetValue(settingname);
            }
        }

        bool haddecimals = false;

        if (fadestep < 1)
        {
            haddecimals = true;
            fadestart *= 100;
            fadeend *= 100;
            fadestep *= 100;
            delay /= (double)100;
        }

        JObject fields;
        if (fadestart < fadeend)
        {
            for (int a = fadestart; a <= fadeend; a += fadestep)
            {
                fields = new JObject();
                fields.Add("sourceName", source);
                fields.Add("filterName", filtername);
                JObject tmpfield = new JObject();
                if (haddecimals == true)
                    tmpfield.Add(settingname, ConvertToType(a / (double)100));
                else
                    tmpfield.Add(settingname, ConvertToType(a));
                fields.Add("filterSettings", tmpfield);
                _obs.SendRequest("SetSourceFilterSettings", fields);
                System.Threading.Thread.Sleep(delay);
            }
        }
        else if (fadestart > fadeend)
        {
            for (int a = fadestart; a >= fadeend; a += -fadestep)
            {
                fields = new JObject();
                fields.Add("sourceName", source);
                fields.Add("filterName", filtername);
                JObject tmpfield = new JObject();
                if (haddecimals == true)
                    tmpfield.Add(settingname, ConvertToType(a / (double)100));
                else
                    tmpfield.Add(settingname, ConvertToType(a));
                fields.Add("filterSettings", tmpfield);
                _obs.SendRequest("SetSourceFilterSettings", fields);
                System.Threading.Thread.Sleep(delay);
            }
        }
    }

    private static void OBSSetVolume(string source, int volume, int delay = 0, int steps = 1)
    {
        volume += 1;
        if (steps < 1)
            steps = 1;
        if (steps > 99)
            steps = 99;
        if (delay == 0)
        {
            double molvol = Math.Pow(volume, 3) / 1000000; // Convert percent to amplitude/mul (approximate, mul is non-linear)
            JObject fields = new JObject();
            fields.Add("inputName", source);
            fields.Add("inputVolumeMul", molvol);
            _obs.SendRequest("SetInputVolume", fields);
        }
        else
        {
            if (delay < 5)
                delay = 5;
            if (delay > 1000)
                delay = 1000;
            JObject fields = new JObject();
            fields.Add("inputName", source);
            JObject _VolumeInfo = _obs.SendRequest("GetInputVolume", fields);

            int startvolume = Math.Pow(System.Convert.ToDouble(_VolumeInfo.GetValue("inputVolumeMul")), 1.0 / 3) * 100; // Convert amplitude/mul to percent (approximate, mul is non-linear)

            if (startvolume == volume)
                return;
            else if (startvolume < volume)
            {
                for (var a = startvolume; a <= volume; a += steps)
                {
                    fields = new JObject();
                    fields.Add("inputName", source);
                    fields.Add("inputVolumeMul", System.Convert.ToDouble(Math.Pow(a, 3) / 1000000));
                    _obs.SendRequest("SetInputVolume", fields);
                    System.Threading.Thread.Sleep(delay);
                }
            }
            else if (startvolume > volume)
            {
                for (var a = startvolume; a >= volume; a += -steps)
                {
                    fields = new JObject();
                    fields.Add("inputName", source);
                    fields.Add("inputVolumeMul", System.Convert.ToDouble(Math.Pow(a, 3) / 1000000));
                    _obs.SendRequest("SetInputVolume", fields);
                    System.Threading.Thread.Sleep(delay);
                }
            }
        }
    }

    private static void PrintUsage()
    {
        List<string> @out = new List<string>();

        @out.Add("OBSCommand v1.6.3 (for OBS Version 28.x.x and above / Websocket 5.x.x and above) ©2018-2022 by FSC-SOFT (http://www.VoiceMacro.net)");
        @out.Add(Constants.vbCrLf);
        @out.Add("Usage:");
        @out.Add("------");
        @out.Add("OBSCommand /server=127.0.0.1:4455 /password=xxxx /delay=0.5 /setdelay=0.05 /profile=myprofile /scene=myscene /hidesource=myscene/mysource /showsource=myscene/mysource /togglesource=myscene/mysource /toggleaudio=myaudio /mute=myaudio /unmute=myaudio /setvolume=mysource,volume,[delay],[steps] /fadeopacity=mysource,myfiltername,startopacity,endopacity,[fadedelay],[fadestep] /slidesetting=mysource,myfiltername,startvalue,endvalue,[slidedelay],[slidestep] /slideasync=mysource,myfiltername,startvalue,endvalue,[slidedelay],[slidestep] /startstream /stopstream /startrecording /stoprecording /command=mycommand,myparam1=myvalue1... /sendjson=jsonstring");
        @out.Add(Constants.vbCrLf);
        @out.Add("Note: If Server is omitted, default 127.0.0.1:4455 will be used.");
        @out.Add("Use quotes if your item name includes spaces.");
        @out.Add("Password can be empty if no password is set in OBS Studio.");
        @out.Add("You can use the same option multiple times.");
        @out.Add("If you use Server and Password, those must be the first 2 options!");
        @out.Add(Constants.vbCrLf);
        @out.Add("This tool uses the obs-websocket plugin to talk to OBS Studio:");
        @out.Add("https://github.com/Palakis/obs-websocket/releases");
        @out.Add(Constants.vbCrLf);
        @out.Add("3rd Party Dynamic link libraries used:");
        @out.Add("Json.NET ©2021 by James Newton-King");
        @out.Add("websocket-sharp ©2010-2022 by BarRaider");
        @out.Add("obs-websocket-dotnet ©2022 by Stéphane Lepin.");
        @out.Add(Constants.vbCrLf);
        @out.Add("Examples:");
        @out.Add("---------");
        @out.Add("OBSCommand /scene=myscene");
        @out.Add("OBSCommand /toggleaudio=\"Desktop Audio\"");
        @out.Add("OBSCommand /mute=myAudioSource");
        @out.Add("OBSCommand /unmute=\"my Audio Source\"");
        @out.Add("OBSCommand /setvolume=Mic/Aux,0,50,2");
        @out.Add("OBSCommand /setvolume=Mic/Aux,100");
        @out.Add("OBSCommand /fadeopacity=Mysource,myfiltername,0,100,5,2");
        @out.Add("OBSCommand /slidesetting=Mysource,myfiltername,contrast,-2,0,100,0.01");
        @out.Add("OBSCommand /slideasync=Mysource,myfiltername,saturation,*,5,100,0.1");
        @out.Add("OBSCommand /stopstream");
        @out.Add("OBSCommand /profile=myprofile /scene=myscene /showsource=mysource");
        @out.Add("OBSCommand /showsource=mysource");
        @out.Add("OBSCommand /hidesource=myscene/mysource");
        @out.Add("OBSCommand /togglesource=myscene/mysource");
        @out.Add("OBSCommand /showsource=\"my scene\"/\"my source\"");
        @out.Add("");
        @out.Add("For most of other simpler requests, use the generalized '/command' feature (see syntax below):");
        @out.Add("OBSCommand /command=SaveReplayBuffer");
        @out.Add(@"OBSCommand /command=SaveSourceScreenshot,sourceName=MyScene,imageFormat=png,imageFilePath=C:\OBSTest.png");
        @out.Add("OBSCommand /command=SetSourceFilterSettings,sourceName=\"Color Correction\",filterName=Opacity,filterSettings=opacity=10");
        @out.Add("OBSCommand /command=SetInputSettings,inputName=\"Browser\",inputSettings=url='https://www.google.com/search?q=query+goes+there'");
        @out.Add("");
        @out.Add("For more complex requests, use the generalized '/sendjson' feature:");
        @out.Add(@"OBSCommand.exe /sendjson=SaveSourceScreenshot={'sourceName':'MyScource','imageFormat':'png','imageFilePath':'H:\\OBSScreenShot.png'}");
        @out.Add("");
        @out.Add("You can combine multiple commands like this:");
        @out.Add("OBSCommand /scene=mysource1 /delay=1.555 /scene=mysource2 ...etc");
        @out.Add("OBSCommand /setdelay=1.555 /scene=mysource1 /scene=mysource2 ...etc");
        @out.Add(Constants.vbCrLf);
        @out.Add("Options:");
        @out.Add("--------");
        @out.Add("/server=127.0.0.1:4455            define server address and port");
        @out.Add("  Note: If Server is omitted, default 127.0.0.1:4455 will be used.");
        @out.Add("/password=xxxx                    define password (can be omitted)");
        @out.Add("/delay=n.nnn                      delay in seconds (0.001 = 1 ms)");
        @out.Add("/setdelay=n.nnn                   global delay in seconds (0.001 = 1 ms)");
        @out.Add("                                  (set it to 0 to cancel it)");
        @out.Add("/profile=myprofile                switch to profile \"myprofile\"");
        @out.Add("/scene=myscene                    switch to scene \"myscene\"");
        @out.Add("/hidesource=myscene/mysource      hide source \"scene/mysource\"");
        @out.Add("/showsource=myscene/mysource      show source \"scene/mysource\"");
        @out.Add("/togglesource=myscene/mysource    toggle source \"scene/mysource\"");
        @out.Add("  Note:  if scene is omitted, current scene is used");
        @out.Add("/toggleaudio=myaudio              toggle mute from audio source \"myaudio\"");
        @out.Add("/mute=myaudio                     mute audio source \"myaudio\"");
        @out.Add("/unmute=myaudio                   unmute audio source \"myaudio\"");
        @out.Add("/setvolume=myaudio,volume,delay,  set volume of audio source \"myaudio\"");
        @out.Add("steps                             volume is 0-100, delay is in milliseconds");
        @out.Add("                                  between steps (min. 5, max. 1000) for fading");
        @out.Add("                                  steps is (1-99), default step is 1");
        @out.Add("  Note:  if delay is omitted volume is set instant");
        @out.Add("/fadeopacity=mysource,myfiltername,startopacity,endopacity,[fadedelay],[fadestep]");
        @out.Add("                                  start/end opacity is 0-100, 0=fully transparent");
        @out.Add("                                  delay is in milliseconds, step 0-100");
        @out.Add("             Note: Use * for start- or endopacity for fade from/to current value");
        @out.Add("/slidesetting=mysource,myfiltername,settingname,startvalue,endvalue,[slidedelay],[slidestep]");
        @out.Add("                                  start/end value min/max depends on setting!");
        @out.Add("                                  delay is in milliseconds");
        @out.Add("                                  step depends on setting (can be x Or 0.x Or 0.0x)");
        @out.Add("             Note: Use * for start- or end value to slide from/to current value");
        @out.Add("/slideasync");
        @out.Add("            The same as slidesetting, only this one runs asynchron!");
        @out.Add("/startstream                      starts streaming");
        @out.Add("/stopstream                       stop streaming");
        @out.Add("/startrecording                   starts recording");
        @out.Add("/stoprecording                    stops recording");
        @out.Add("");
        @out.Add("General User Command syntax:");
        @out.Add("----------------------------");
        @out.Add("/command=mycommand,myparam1=myvalue1,myparam2=myvalue2...");
        @out.Add("                                  issues user command,parameter(s) (optional)");
        @out.Add("/command=mycommand,myparam1=myvalue1,myparam2=myvalue2,myparam3=mysubparam=mysubparamvalue");
        @out.Add("                                  issues user command,parameters and sub-parameters");
        @out.Add("");
        @out.Add("A full list of commands is available here https://github.com/obsproject/obs-websocket/blob/master/docs/generated/protocol.md");
        @out.Add("");

        int i = 0;
        int z = 0;

        while (true)
        {
            Console.WriteLine(@out[i]);
            if (z > Console.WindowHeight - 1)
            {
                Console.Write("Press any key to continue...");
                Console.ReadKey();
                ClearCurrentConsoleLine();
                z = 0;
            }
            i += 1;
            z += 1;
            if (i >= @out.Count)
                break;

            z += @out[i].Length / (Console.WindowWidth / (double)2);
        }
    }

    private static string[] SplitWhilePreservingQuotedValues(string value, char delimiter, bool DeleteQuotes = false)
    {
        Regex csvPreservingQuotedStrings = new Regex(string.Format("(\"[^\"]*\"|[^{0}])+", delimiter));
        var values = csvPreservingQuotedStrings.Matches(value).Cast<Match>().Select(m => m.Value.TrimStart(" ")).Where(v => !string.IsNullOrEmpty(v));

        string[] tmp = values.ToArray();

        if (DeleteQuotes == false)
            return tmp;

        for (var a = 0; a <= tmp.Length - 1; a++)
        {
            if (tmp[a] != Strings.Chr(34) && tmp[a].StartsWith(Strings.Chr(34)) & tmp[a].EndsWith(Strings.Chr(34)))
                tmp[a] = tmp[a].Substring(1, tmp[a].Length - 2);
        }

        return tmp;
    }

    public static void ClearCurrentConsoleLine()
    {
        int currentLineCursor = Console.CursorTop;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, currentLineCursor);
    }





    // Class for Async Slide Settings
    class AsyncSlideSettings
    {
        private string _server;
        private string _password;
        private string _source;
        private string _filtername;
        private string _settingname;
        private string _fadestart;
        private string _fadeend;
        private int _delay;
        private string _fadestep;

        public AsyncSlideSettings(string server, string password, string source, string filtername, string settingname, string fadestart, string fadeend, int delay = 0, string fadestep = "1")
        {
            _server = server;
            _password = password;
            _source = source;
            _filtername = filtername;
            _settingname = settingname;
            _fadestart = fadestart;
            _fadeend = fadeend;
            _delay = delay;
            _fadestep = fadestep;
        }

        public void StartSlide()
        {
            SlideSetting(_server, _password, _source, _filtername, _settingname, _fadestart, _fadeend, _delay, _fadestep);
        }

        public void SlideSetting(string server, string password, string source, string filtername, string settingname, string fadestart, string fadeend, int delay = 0, string fadestep = "1")
        {
            var obs = new OBSWebsocket();
            obs.WSTimeout = new TimeSpan(0, 0, 0, 3);
            obs.ConnectAsync(server, password);
            int i = 0;
            while (!obs.IsConnected)
            {
                System.Threading.Thread.Sleep(10);
                i += 1;
                if (i > 300)
                {
                    Console.Write("Error: can't connect to OBS websocket plugin!");
                    System.Environment.Exit(0);
                }
            }

            if (delay < 5)
                delay = 5;
            if (delay > 1000)
                delay = 1000;

            if (fadestart == "*" | fadeend == "*")
            {
                JObject tmpfield = new JObject();
                tmpfield.Add("sourceName", source);
                tmpfield.Add("filterName", filtername);
                JObject result = obs.SendRequest("GetSourceFilter", tmpfield);

                if (fadestart == "*")
                {
                    JObject tmp = result.GetValue("filterSettings");
                    fadestart = tmp.GetValue(settingname);
                }
                else if (fadeend == "*")
                {
                    JObject tmp = result.GetValue("filterSettings");
                    fadeend = tmp.GetValue(settingname);
                }
            }

            bool haddecimals = false;

            if (fadestep < 1)
            {
                haddecimals = true;
                fadestart *= 100;
                fadeend *= 100;
                fadestep *= 100;
                delay /= (double)100;
            }

            JObject fields;
            if (fadestart < fadeend)
            {
                for (int a = fadestart; a <= fadeend; a += fadestep)
                {
                    fields = new JObject();
                    fields.Add("sourceName", source);
                    fields.Add("filterName", filtername);
                    JObject tmpfield = new JObject();
                    if (haddecimals == true)
                        tmpfield.Add(settingname, ConvertToType(a / (double)100));
                    else
                        tmpfield.Add(settingname, ConvertToType(a));
                    fields.Add("filterSettings", tmpfield);
                    obs.SendRequest("SetSourceFilterSettings", fields);
                    System.Threading.Thread.Sleep(delay);
                }
            }
            else if (fadestart > fadeend)
            {
                for (int a = fadestart; a >= fadeend; a += -fadestep)
                {
                    fields = new JObject();
                    fields.Add("sourceName", source);
                    fields.Add("filterName", filtername);
                    JObject tmpfield = new JObject();
                    if (haddecimals == true)
                        tmpfield.Add(settingname, ConvertToType(a / (double)100));
                    else
                        tmpfield.Add(settingname, ConvertToType(a));
                    fields.Add("filterSettings", tmpfield);
                    obs.SendRequest("SetSourceFilterSettings", fields);
                    System.Threading.Thread.Sleep(delay);
                }
            }

            _obs.Disconnect();
        }
    }
}
