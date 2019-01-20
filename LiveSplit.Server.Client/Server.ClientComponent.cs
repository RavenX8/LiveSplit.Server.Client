using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Windows.Forms;
using LiveSplit.Model;
using LiveSplit.Options;
using LiveSplit.UI;
using LiveSplit.UI.Components;

namespace LiveSplit.Server.Client
{
  class Server.ClientComponent : IComponent
  {
    private Task _thread;
    private CancellationTokenSource _cancelSource;

    protected Form Form { get; set; }
    private ComponentSettings _settings;
    private LiveSplitState _state;
    public static Socket Socket { get; set; }

    public string ComponentName => $"Autosplitter Client ({ (Socket.Connected ? "Connected" : "Disconnected") })";

    /// <inheritdoc />
    public float HorizontalWidth { get; }

    /// <inheritdoc />
    public float MinimumHeight { get; }

    /// <inheritdoc />
    public float VerticalHeight { get; }

    /// <inheritdoc />
    public float MinimumWidth { get; }

    /// <inheritdoc />
    public float PaddingTop { get; }

    /// <inheritdoc />
    public float PaddingBottom { get; }

    /// <inheritdoc />
    public float PaddingLeft { get; }

    /// <inheritdoc />
    public float PaddingRight { get; }

    public IDictionary<string, Action> ContextMenuControls { get; protected set; }

    public Server.ClientComponent(LiveSplitState state)
    {
      _state = state;
      Form = state.Form;
      _settings = new ComponentSettings();
      Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

      ContextMenuControls = new Dictionary<string, Action>();
      ContextMenuControls.Add("Connect to server", Start);

      _state.OnStart += gameProcess_OnStart;
      _state.OnReset += gameProcess_OnReset;
      _state.OnSplit += gameProcess_OnSplit;
      _state.OnUndoSplit += gameProcess_OnUndoSplit;
      _state.OnPause += gameProcess_OnPause;
      _state.OnResume += gameProcess_OnResume;
      _state.OnSwitchComparisonNext += gameProcess_OnSwitchComparison;
      _state.OnSwitchComparisonPrevious += gameProcess_OnSwitchComparison;

      if (_thread != null && _thread.Status == TaskStatus.Running)
        throw new InvalidOperationException();

      _cancelSource = new CancellationTokenSource();
      _thread = Task.Factory.StartNew(UpdateThread);
    }

    public void Dispose()
    {
      if (_cancelSource != null && _thread != null && _thread.Status == TaskStatus.Running)
      {
        _cancelSource.Cancel();
        _thread.Wait();
      }
      ContextMenuControls.Clear();

      _state.OnResume -= gameProcess_OnResume;
      _state.OnPause -= gameProcess_OnPause;
      _state.OnUndoSplit -= gameProcess_OnUndoSplit;
      _state.OnSplit -= gameProcess_OnSplit;
      _state.OnReset -= gameProcess_OnReset;
      _state.OnStart -= gameProcess_OnStart;

      if (Socket.Connected == true)
      {
        Socket.Shutdown(SocketShutdown.Both);
      }
      Socket.Close();
    }

    public void Start()
    {
      try
      {
        IPEndPoint ipe = new IPEndPoint(_settings.IpAddress, _settings.Port);
        Socket.Connect(ipe);

        byte[] msg = System.Text.Encoding.ASCII.GetBytes("initgametime\r\n");
        Socket.Send(msg);

        ContextMenuControls.Clear();
        ContextMenuControls.Add("Disconnect from server", Stop);
      }
      catch (ArgumentNullException ae)
      {
        Console.WriteLine("ArgumentNullException : {0}", ae.ToString());
      }
      catch (SocketException se)
      {
        Console.WriteLine("SocketException : {0}", se.ToString());
      }
      catch (Exception ex)
      {
        Console.WriteLine("Unexpected exception : {0}", ex.ToString());
      }
    }

    public void Stop()
    {
      if (Socket.Connected == true)
      {
        Socket.Shutdown(SocketShutdown.Both);
      }

      ContextMenuControls.Clear();
      ContextMenuControls.Add("Connect to server", Start);
    }

    private void gameProcess_OnStart(object sender, EventArgs e)
    {
      if (Socket.Connected == false)
        return;

      byte[] msg = System.Text.Encoding.ASCII.GetBytes("starttimer\r\n");
      Form.BeginInvoke(new Action(() => Socket.Send(msg)));
    }

    private void gameProcess_OnReset(object sender, TimerPhase value)
    {
      if (Socket.Connected == false)
        return;

      byte[] msg = System.Text.Encoding.ASCII.GetBytes("reset\r\n");
      Form.BeginInvoke(new Action(() => Socket.Send(msg)));
    }

    private void gameProcess_OnSplit(object sender, EventArgs e)
    {
      if (Socket.Connected == false)
        return;

      byte[] msg = System.Text.Encoding.ASCII.GetBytes("split\r\n");
      Form.BeginInvoke(new Action(() => Socket.Send(msg)));
    }

    private void gameProcess_OnUndoSplit(object sender, EventArgs e)
    {
      if (Socket.Connected == false)
        return;

      byte[] msg = System.Text.Encoding.ASCII.GetBytes("unsplit\r\n");
      Form.BeginInvoke(new Action(() => Socket.Send(msg)));
    }

    void gameProcess_OnPause(object sender, EventArgs e)
    {
      if (Socket.Connected == false)
        return;

      byte[] msg = System.Text.Encoding.ASCII.GetBytes("pause\r\n");
      Form.BeginInvoke(new Action(() => Socket.Send(msg)));
    }

    private void gameProcess_OnResume(object sender, EventArgs e)
    {
      if (Socket.Connected == false)
        return;

      byte[] msg = System.Text.Encoding.ASCII.GetBytes("resume\r\n");
      Form.BeginInvoke(new Action(() => Socket.Send(msg)));
    }

    private void gameProcess_OnSwitchComparison(object sender, EventArgs e)
    {
      if (Socket.Connected == false)
        return;

      byte[] msg = System.Text.Encoding.ASCII.GetBytes("setcomparison  " + _state.CurrentComparison + "\r\n");
      Form.BeginInvoke(new Action(() => Socket.Send(msg)));
    }

    /// <inheritdoc />
    public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
    {
    }

    /// <inheritdoc />
    public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
    {
    }

    public Control GetSettingsControl(LayoutMode mode)
    {
      return _settings;
    }

    public XmlNode GetSettings(XmlDocument document)
    {
      return _settings.GetSettings(document);
    }

    public void SetSettings(XmlNode settings)
    {
      _settings.SetSettings(settings);
    }

    public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height,
                       LayoutMode mode)
    {
    }


    private void UpdateThread()
    {
      byte[] msg;
      while (!_cancelSource.IsCancellationRequested)
      {
        try
        {
          while (true)
          {
            if (Socket.Connected == true)
            {
              if (_state.CurrentPhase == TimerPhase.Running)
              {
                if (_state.IsGameTimePaused == true)
                {
                  msg = System.Text.Encoding.ASCII.GetBytes("pausegametime\r\n");
                  Form.BeginInvoke(new Action(() => Socket.Send(msg)));
                }
                else
                {
                  msg = System.Text.Encoding.ASCII.GetBytes("unpausegametime\r\n");
                  Form.BeginInvoke(new Action(() => Socket.Send(msg)));
                }

                //TODO: Figure out why the game time is all messed up
                msg = System.Text.Encoding.ASCII.GetBytes("setgametime " + _state.CurrentTime.GameTime.ToString() + "\r\n");
                Form.BeginInvoke(new Action(() => Socket.Send(msg)));

                msg = System.Text.Encoding.ASCII.GetBytes("setloadingtimes " + _state.LoadingTimes.TotalMilliseconds.ToString() + "\r\n");
                Form.BeginInvoke(new Action(() => Socket.Send(msg)));
              }
            }

            Thread.Sleep(250);

            if (_cancelSource.IsCancellationRequested)
              return;
          }
        }
        catch (Exception ex)
        {
          Trace.WriteLine(ex.ToString());
          Thread.Sleep(1000);
        }
      }
    }
  }
}
