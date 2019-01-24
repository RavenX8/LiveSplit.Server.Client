using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.Server.Client;

namespace LiveSplit.UI.Components
{
    public partial class ComponentSettings : UserControl
  {
    private Label label1;
    private TextBox ipAddressTextBox;
    private Label label2;
    private Button connectButton;
    private GroupBox groupBox1;
    private TextBox portTextBox;

    public int Port { get; set; }
    public IPAddress IpAddress { get; set; }

    private Dictionary<string, CheckBox> _basic_settings;

    // Start/Reset/Split checkboxes
    private Dictionary<string, bool> _basic_settings_state;

    // Custom settings
    private Dictionary<string, bool> _custom_settings_state;

    public ComponentSettings()
    {
      InitializeComponent();

      _basic_settings = new Dictionary<string, CheckBox>();

      _basic_settings_state = new Dictionary<string, bool>();
      _custom_settings_state = new Dictionary<string, bool>();
    }

    public XmlNode GetSettings(XmlDocument document)
    {
      XmlElement settings_node = document.CreateElement("Settings");
      settings_node.AppendChild(SettingsHelper.ToElement(document, "Version", "1.0"));
      settings_node.AppendChild(SettingsHelper.ToElement(document, "IPAddress", this.ipAddressTextBox.Text));
      settings_node.AppendChild(SettingsHelper.ToElement(document, "Port", this.portTextBox.Text));

      AppendBasicSettingsToXml(document, settings_node);
      AppendCustomSettingsToXml(document, settings_node);

      return settings_node;
    }

    private void AppendBasicSettingsToXml(XmlDocument document, XmlElement settings_node)
    {
      if (_basic_settings != null && _basic_settings.Count > 0)
        foreach (var item in _basic_settings)
        {
          if (_basic_settings_state.ContainsKey(item.Key.ToLower()))
          {
            var value = _basic_settings_state[item.Key.ToLower()];
            settings_node.AppendChild(SettingsHelper.ToElement(document, item.Key, value));
          }
        }
    }

    private void AppendCustomSettingsToXml(XmlDocument document, XmlElement parent)
    {
      XmlElement Client_parent = document.CreateElement("CustomSettings");

      foreach (var setting in _custom_settings_state)
      {
        XmlElement element = SettingsHelper.ToElement(document, "Setting", setting.Value);
        XmlAttribute id = SettingsHelper.ToAttribute(document, "id", setting.Key);
        // In case there are other setting types in the future
        XmlAttribute type = SettingsHelper.ToAttribute(document, "type", "bool");

        element.Attributes.Append(id);
        element.Attributes.Append(type);
        Client_parent.AppendChild(element);
      }

      parent.AppendChild(Client_parent);
    }

    // Loads the settings of this component from Xml. This might happen more than once
    // (e.g. when the settings dialog is cancelled, to restore previous settings).
    public void SetSettings(XmlNode settings)
    {
      var element = (XmlElement)settings;
      if (!element.IsEmpty)
      {
        this.ipAddressTextBox.Text = SettingsHelper.ParseString(element["IPAddress"], string.Empty);
        this.portTextBox.Text = SettingsHelper.ParseString(element["Port"], "16834");

        ParseBasicSettingsFromXml(element);
        ParseCustomSettingsFromXml(element);


        if (this.ipAddressTextBox.Text.Length >= 7)
        {
          IpAddress = IPAddress.Parse(this.ipAddressTextBox.Text);
        }

        if (this.portTextBox.Text.Length > 1)
        {
          Port = int.Parse(this.portTextBox.Text);
        }
      }
    }

    private void ParseBasicSettingsFromXml(XmlElement element)
    {
      foreach (var item in _basic_settings)
      {
        if (element[item.Key] != null)
        {
          var value = bool.Parse(element[item.Key].InnerText);

          // If component is not enabled, don't check setting
          if (item.Value.Enabled)
            item.Value.Checked = value;

          _basic_settings_state[item.Key.ToLower()] = value;
        }
      }
    }

    /// <summary>
    /// Parses custom settings, stores them and updates the checked state of already added tree nodes.
    /// </summary>
    /// 
    private void ParseCustomSettingsFromXml(XmlElement data)
    {
      XmlElement custom_settings_node = data["CustomSettings"];

      if (custom_settings_node != null && custom_settings_node.HasChildNodes)
      {
        foreach (XmlElement element in custom_settings_node.ChildNodes)
        {
          if (element.Name != "Setting")
            continue;

          string id = element.Attributes["id"].Value;
          string type = element.Attributes["type"].Value;

          if (id != null && type == "bool")
          {
            bool value = SettingsHelper.ParseBool(element);
            _custom_settings_state[id] = value;
          }
        }
      }
    }

    private void InitializeComponent()
    {
      this.label1 = new System.Windows.Forms.Label();
      this.ipAddressTextBox = new System.Windows.Forms.TextBox();
      this.label2 = new System.Windows.Forms.Label();
      this.portTextBox = new System.Windows.Forms.TextBox();
      this.connectButton = new System.Windows.Forms.Button();
      this.groupBox1 = new System.Windows.Forms.GroupBox();
      this.groupBox1.SuspendLayout();
      this.SuspendLayout();
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(6, 20);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(58, 13);
      this.label1.TabIndex = 0;
      this.label1.Text = "IP Address";
      // 
      // ipAddressTextBox
      // 
      this.ipAddressTextBox.Location = new System.Drawing.Point(70, 17);
      this.ipAddressTextBox.MaxLength = 15;
      this.ipAddressTextBox.Name = "ipAddressTextBox";
      this.ipAddressTextBox.Size = new System.Drawing.Size(100, 20);
      this.ipAddressTextBox.TabIndex = 0;
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(38, 42);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(26, 13);
      this.label2.TabIndex = 2;
      this.label2.Text = "Port";
      // 
      // portTextBox
      // 
      this.portTextBox.Location = new System.Drawing.Point(70, 39);
      this.portTextBox.MaxLength = 5;
      this.portTextBox.Name = "portTextBox";
      this.portTextBox.Size = new System.Drawing.Size(100, 20);
      this.portTextBox.TabIndex = 1;
      this.portTextBox.Text = "16834";
      // 
      // connectButton
      // 
      this.connectButton.Location = new System.Drawing.Point(95, 65);
      this.connectButton.Name = "connectButton";
      this.connectButton.Size = new System.Drawing.Size(75, 23);
      this.connectButton.TabIndex = 3;
      this.connectButton.Text = "Connect";
      this.connectButton.UseVisualStyleBackColor = true;
      this.connectButton.Click += new System.EventHandler(this.connectButton_Click);
      // 
      // groupBox1
      // 
      this.groupBox1.Controls.Add(this.label1);
      this.groupBox1.Controls.Add(this.connectButton);
      this.groupBox1.Controls.Add(this.ipAddressTextBox);
      this.groupBox1.Controls.Add(this.portTextBox);
      this.groupBox1.Controls.Add(this.label2);
      this.groupBox1.Location = new System.Drawing.Point(3, 3);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(184, 100);
      this.groupBox1.TabIndex = 0;
      this.groupBox1.TabStop = false;
      this.groupBox1.Text = "Server Connection Info";
      // 
      // ComponentSettings
      // 
      this.Controls.Add(this.groupBox1);
      this.Name = "ComponentSettings";
      this.Size = new System.Drawing.Size(197, 115);
      this.groupBox1.ResumeLayout(false);
      this.groupBox1.PerformLayout();
      this.ResumeLayout(false);

    }

    private void connectButton_Click(object sender, EventArgs e)
    {
      connectButton.BackColor = DefaultBackColor;
      if (ServerClientComponent.Socket.Connected == true)
      {
        ServerClientComponent.Socket.Disconnect(true);
      }
      else
      {
        if (this.ipAddressTextBox.Text.Length >= 7)
        {
          IpAddress = IPAddress.Parse(this.ipAddressTextBox.Text);
        }
        else
        {
          connectButton.BackColor = Color.Yellow;
          return;
        }

        if (this.portTextBox.Text.Length > 1)
        {
          Port = int.Parse(this.portTextBox.Text);
        }

        try
        {
          IPEndPoint ipe = new IPEndPoint(IpAddress, Port);
          ServerClientComponent.Socket.Connect(ipe);
          connectButton.BackColor = Color.Green;

          byte[] msg = System.Text.Encoding.ASCII.GetBytes("initgametime\r\n");
          ServerClientComponent.Socket.Send(msg);
        }
        catch (ArgumentNullException ae)
        {
          connectButton.BackColor = Color.Red;
          Console.WriteLine("ArgumentNullException : {0}", ae.ToString());
        }
        catch (SocketException se)
        {
          connectButton.BackColor = Color.Red;
          Console.WriteLine("SocketException : {0}", se.ToString());
        }
        catch (Exception ex)
        {
          connectButton.BackColor = Color.Red;
          Console.WriteLine("Unexpected exception : {0}", ex.ToString());
        }
      }
    }
  }
}
