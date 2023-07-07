using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net;
using System.Text;
using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Events;
using FlexRadioServices.Models;
using FlexRadioServices.Models.Ports;
using FlexRadioServices.Models.Ports.Network;
using FlexRadioServices.Utils.Slice;

namespace FlexRadioServices.Services;

public class FlexCatPortService : ConnectedRadioServiceBase, ICatPortService
{
    private readonly PortSettings _portSettings;
    private readonly ITcpServer _tcpServer;
    private readonly ILogger<FlexCatPortService> _logger;
    private CancellationTokenSource? _cancellationToken;
    private Task? _serverTask;
    private Slice? _slice;
    private Slice? _slice2 = null;
    private bool _split;  //placeholder
    private readonly ConcurrentQueue<CatCommand> _commandQueue;
    private bool _autoInfo;
    private bool _shortFreq;
    private double _splitFreq;
    private bool _splitXit;

    private readonly StringBuilder _commandDataBuffer = new();
    
    public FlexCatPortService(PortSettings portSettings, ITcpServer tcpServer,
        ILogger<FlexCatPortService> logger, IFlexRadioService flexRadioService) : base(flexRadioService, logger)
    {
        _logger = logger;
        _portSettings = portSettings;
        _tcpServer = tcpServer;
        _commandQueue = new ConcurrentQueue<CatCommand>();
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting CAT on port {Port} ", _portSettings.PortNumber);
        _cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _serverTask = _tcpServer.StartListener(IPAddress.Any, _portSettings.PortNumber, cancellationToken);
        _tcpServer.ClientConnected += TcpServerOnClientConnected;
        _tcpServer.ClientDisconnected += TcpServerOnClientDisconnected;

        if (_serverTask.IsCompleted)
        {
            return _serverTask;
        }

        _logger.LogDebug("Tcp Server started for {Port}", _portSettings.PortNumber);
        return base.StartAsync(cancellationToken);
    }

    protected override async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        if (_commandQueue.TryDequeue(out var command))
        {
            var response = ProcessCommand(command.Command);
            if (command.Command.StartsWith("IF"))
            {
                _logger.LogDebug("Sending {Response} for command {Command} to requesting client",response, command.Command);
            }

            try
            {
                if (command.Client.Connected)
                {
                    await command.Client.SendAsync(response);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,"Error sending response to client");
            }
            
            return;
        }
        
        await Task.Delay(100, cancellationToken);
    }

    private void TcpServerOnClientDisconnected(object? sender, ClientDisconnectedEventArgs e)
    {
        _logger.LogDebug("Removing data received listener {Port}", _portSettings.PortNumber);
        e.Client.DataReceived -= ClientOnDataReceived;
    }

    private void TcpServerOnClientConnected(object? sender, ClientConnectedEventArgs e)
    {
        _logger.LogDebug("Adding data received listener {Port}", _portSettings.PortNumber);
        e.Client.DataReceived += ClientOnDataReceived;
    }

    private void ClientOnDataReceived(object? sender, DataReceivedEventArgs e)
    {
        _logger.LogTrace("Data received from client {Data}", e.Data);
        if (sender is ITcpServerClient client)
        {
            var data = e.Data.Replace("\r", "").Replace("\n", "");
            _commandDataBuffer.Append(data);
            foreach (string command in ExtractCommandsFromBuffer(_commandDataBuffer))
            {
                if (!command.StartsWith("F"))
                {
                    _logger.LogDebug("Adding command {Command} to queue", command);
                }
                _commandQueue.Enqueue(new CatCommand(command, client));
            }
        }
    }
    
    private List<string> ExtractCommandsFromBuffer(StringBuilder buffer)
    {
        List<string> stringList = new List<string>();
        string s = buffer.ToString();
        do
        {
            var length = s.IndexOf(";", StringComparison.Ordinal);
            if (length >= 0)
            {
                if (length > 0)
                {
                    string command = s.Substring(0, length);
                    stringList.Add(command);
                }
                s = s.Remove(0, length + 1);
                buffer.Remove(0, length + 1);
            }
            else
                break;
        }
        while (true);
        return stringList;
    }


    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop called without start
        if (_serverTask == null)
        {
            return;
        }

        try
        {
            // Signal cancellation to the executing method
            _cancellationToken!.Cancel();
        }
        finally
        {
            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(_serverTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
        }
    }

    protected override void ConnectedRadioChanged(object? sender, ConnectedRadioEventArgs args)
    {
        if (args.PreviousRadio != null)
        {
            foreach (var slice in args.PreviousRadio.Radio.SliceList)
            {
                RadioOnSliceRemoved(slice);
            }

            args.PreviousRadio.Radio.SliceAdded -= RadioOnSliceAdded;
            args.PreviousRadio.Radio.SliceRemoved -= RadioOnSliceRemoved;
            _slice = null;
        }

        if (ConnectedRadio != null)
        {
            foreach (var slice in ConnectedRadio.Radio.SliceList)
            {
                RadioOnSliceAdded(slice);
            }

            ConnectedRadio.Radio.SliceAdded += RadioOnSliceAdded;
            ConnectedRadio.Radio.SliceRemoved += RadioOnSliceRemoved;
        }
    }

    private void RadioOnSliceRemoved(Slice slc)
    {
        _logger.LogDebug("Removed slice {Letter} listener for radio {RadioSerial} on port {Port}", 
            slc.Letter, slc.Radio.Serial, _portSettings.PortNumber);
        slc.PropertyChanged -= SliceOnPropertyChanged;
        if (slc == _slice)
        {
            _slice = null;
        }
    }

    private void RadioOnSliceAdded(Slice slc)
    {
        _logger.LogDebug("Added slice {Letter} listener for radio {RadioSerial} on port {Port}", 
            slc.Letter, slc.Radio.Serial, _portSettings.PortNumber);
        slc.PropertyChanged += SliceOnPropertyChanged;

        if (_portSettings.PortSliceType == PortSliceType.Designated && slc.Letter == _portSettings.VfoASliceLetter)
        {
            _slice = slc;
            return;
        }
        
        if (_portSettings.PortSliceType == PortSliceType.Designated && slc.Letter == _portSettings.VfoBSliceLetter)
        {
            _slice2 = slc;
            return;
        }

        if (_portSettings.PortSliceType == PortSliceType.Transmit && slc.IsTransmitSlice)
        {
            _slice = slc;
            return;
        }

        if (_portSettings.PortSliceType == PortSliceType.Active && slc.Active)
        {
            _slice = slc;
        }
    }

    private async void SliceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
       
        if (sender is Slice slice)
        {
            _logger.LogDebug("Slice property {Property} changed {Letter}",e.PropertyName, slice.Letter);
            if (e.PropertyName == nameof(Slice.IsTransmitSlice) &&
                _portSettings.PortSliceType == PortSliceType.Transmit)
            {
                _slice = TransmitSlice;
                _logger.LogDebug("Setting TX _slice to {Letter}", _slice?.Letter);
                await InitiateCommand("IF");
                return;
            }

            if (e.PropertyName == nameof(Slice.Active) &&
                _portSettings.PortSliceType == PortSliceType.Active)
            {
                _slice = ActiveSlice;
                
                await InitiateCommand("IF");
                return;
            }

            if (slice == _slice)
            {
                if (_autoInfo)
                {
                    switch (e.PropertyName)
                    {
                        case nameof(Slice.Freq):
                            await InitiateCommand("FA");
                            break;
                        case nameof(Slice.DemodMode):
                            await InitiateCommand("MD");
                            break;
                    }
                }
            }
        }
    }

    private async Task SendResponseToAllAsync(string command)
    {
        _logger.LogTrace("Sending command {Command} to {Num} client(s)", command, _tcpServer.Clients.Count);
        foreach (var client in _tcpServer.Clients)
        {
            await client.SendAsync(command);
        }
    }

    private async Task InitiateCommand(string command)
    {
        var response = ProcessCommand(command);
        _logger.LogDebug("Initiate response {Response} to port {Port}", response, _portSettings.PortNumber);
        await SendResponseToAllAsync(response);
    } 

    private string ProcessCommand(string input)
    {
        input = input.ToUpper();
        string response = "?;";
        if (input.Length < 2)
            return response;
        string command = input.Substring(0, 2);
        if (command == "ZZ")
        {
            if (input.Length < 4)
                return response;
            command = input.Substring(0, 4);
        }

        switch (command)
        {
            case "AI":
                response = Parse_AI(input);
                break;
            case "FA":
                response = Parse_FA(input);
                break;
            case "FB":
                //str1 = this.Parse_FB(command);
                break;
            case "FR":
                response = Parse_FR(input);
                break;
            case "FT":
                response = Parse_FT(input);
                break;
            case "ID":
                response = Parse_ID(input);
                break;
            case "IF":
                response = Parse_IF(input);
                break;
            case "MD":
                response = Parse_MD(input);
                break;
            case "RX":
                response = Parse_RX(input);
                break;
            case "TX":
                response = Parse_TX(input);
                break;
            case "ZZFA":
                response = Parse_ZZFA(input);
                break;
            case "ZZFB":
                //str1 = Parse_ZZFB(command);
                break;
            case "ZZFI":
                response = Parse_ZZFI(input);
                break;
            case "ZZIF":
                response = Parse_ZZIF(input);
                break;
            case "ZZMD":
                response = Parse_ZZMD(input);
                break;
            case "ZZRX":
                response = Parse_ZZRX(input);
                break;
            default:
                _logger.LogWarning("Command {Cmd} is not implemented", input);
                break;
        }

        return response;
    }

    #region Standard Kenwood

    

    private string Parse_AI(string command)
    {
        string aiResponse = "?;";
        if (command.Length == 2)
        {
            aiResponse = "AI" + Convert.ToByte(_autoInfo) + ";";
        }
        else
        {
            if (command.Length == 3 && int.TryParse(command.Substring(2), out var result))
            {
                _autoInfo = Convert.ToBoolean(result);
                aiResponse = command;
            }
        }

        return aiResponse;
    }


    private string Parse_FA(string command)
    {
        string faResponse = Parse_ZZFA("ZZ" + command);
        if (faResponse != "?;" && faResponse != "")
            faResponse = faResponse.Substring("ZZ".Length);
        return faResponse;
    }
    
    private string Parse_FB(string command)
    {
        string fb = Parse_ZZFB("ZZ" + command);
        if (fb != "?;" && fb != "")
            fb = fb.Substring("ZZ".Length);
        return fb;
    }

    private string Parse_FR(string command)
    {
        string frResponse = "?;";
        switch (command)
        {
            case "FR":
                if (_slice != null && _slice.Active)
                {
                    frResponse = "FR0;";
                    break;
                }

                if (_slice2 != null && _slice2.Active)
                {
                    frResponse = "FR1;";
                }

                break;
            case "FR0":
                if (_slice != null)
                {
                    _slice.Active = true;
                    frResponse = "";
                }

                break;
            case "FR1":
                if (_slice2 != null)
                {
                    _slice2.Active = true;
                    frResponse = "";
                }

                break;
        }

        return frResponse;
    }

    private string Parse_FT(string command)
    {
        string ftResponse = "?;";
        if (_slice != null)
        {
            switch (command)
            {
                case "FT":
                    if (_slice.IsTransmitSlice)
                    {
                        ftResponse = "FT0;";
                        break;
                    }

                    if (_slice2 != null && _slice2.IsTransmitSlice)
                    {
                        ftResponse = "FT1;";
                    }

                    break;
                case "FT0":
                    ftResponse = "";
                    break;
                case "FT1":
                    ftResponse = "";
                    break;
            }
        }

        return ftResponse;
    }

    private string Parse_ID(string command)
    {
        string idResponse = "?;";
        if (ConnectedRadio != null && command.Length == 2)
        {
            int num = 0;
            switch (ConnectedRadio.Radio.Model)
            {
                case "FLEX-6300":
                    num = 907;
                    break;
                case "FLEX-6400":
                case "FLEX-6400M":
                    num = 908;
                    break;
                case "FLEX-6500":
                    num = 905;
                    break;
                case "FLEX-6600":
                case "FLEX-6600M":
                    num = 909;
                    break;
                case "FLEX-6700":
                    num = 904;
                    break;
                case "FLEX-6700R":
                    num = 906;
                    break;
            }

            idResponse = "ID" + num.ToString("D3") + ";";
        }

        return idResponse;
    }
    
    private string Parse_IF(string command)
    {
        string ifResponse = "?;";
        if (command.Length == 2 && _slice != null && ConnectedRadio != null)
        {
            string freq = ((long) Math.Round(_slice.Freq * 1000000.0, 0)).ToString("D11");
            if (freq.Length > 11)
                freq = "99999999999";
            string tuneStep = _slice.TuneStep.ToString("D4");
            if (tuneStep.Length > 4)
                tuneStep = "9999";
            string ritXitFreq = !_slice.XITOn ? _slice.RITFreq.ToString("+00000;-00000") : _slice.XITFreq.ToString("+00000;-00000");
            if (ritXitFreq.Length > 6)
                ritXitFreq = ritXitFreq.Substring(0, 1) + "99999";
            string ritOn = _slice.RITOn ? "1" : "0";
            string xitOn = _slice.XITOn ? "1" : "0";
            string mox = ConnectedRadio.Radio.Mox ? "1" : "0";
            int kwModeNumber = DemodModeToKenwoodModeNumber(_slice.DemodMode);
            string str8 = " ";
            if (kwModeNumber >= 0)
                str8 = kwModeNumber.ToString();
            string str9 = "0";
            if (_slice2 != null && _slice2.Active)
                str9 = "1";
            string txRx = "0";
            if (_slice2 != null && _slice2.IsTransmitSlice)
                txRx = "1";
            ifResponse = "IF" + freq + tuneStep + ritXitFreq + ritOn + xitOn + "000" + mox + str8 + str9 + "0" + txRx + "0000;";
        }
        return ifResponse;
    }
    
    private string Parse_MD(string command)
    {
        string mdResponse = "?;";
        if (_slice != null)
        {
            if (command.Length == 2)
            {
                int kwModeNumber = DemodModeToKenwoodModeNumber(_slice.DemodMode);
                if (kwModeNumber >= 0)
                    mdResponse = "MD" + kwModeNumber + ";";
            }
            else
            {
                if (command.Length == 3 && int.TryParse(command.Substring(2), out var result))
                {
                    string? demodMode = KenwoodModeNumberToDemodMode(result);
                    if (demodMode != null)
                    {
                        _slice.DemodMode = demodMode;
                        mdResponse = "";
                    }
                }
            }
        }
        return mdResponse;
    }
    
    private string Parse_RX(string command)
    {
        string rx = "?;";
        if (command.Length == 2 && ConnectedRadio != null)
        {
            ConnectedRadio.Radio.Mox = false;
            rx = "";
        }

        return rx;
    }

    private string Parse_TX(string command)
    {
        string tx = "?;";
        if (ConnectedRadio != null && _slice != null &&
            (command == "TX" || command == "TX0" || command == "TX1" || command == "TX2"))
        {
            EnsureTransmitSlice();
            ConnectedRadio.Radio.Mox = true;
            tx = "";
        }

        return tx;
    }

    #endregion

    #region Extended Flex
    
    private string Parse_ZZFA(string command)
    {
        string zzfaResponse = "?;";
        if (_slice != null)
        {
            if (command.Length == 4)
            {
                long num = (long)Math.Round(_slice.Freq * 1000000.0, 0);
                string format = "D11";
                if (_shortFreq)
                    format = "D8";
                zzfaResponse = !_shortFreq || num < 100000000.0 ? "ZZFA" + num.ToString(format) + ";" : "?;";
            }
            else if (command.Length == 15)
            {
                if (long.TryParse(command.Substring(4), out var result))
                {
                    _shortFreq = false;
                    _slice.Freq = Math.Round(result * 1E-06, 6);
                    zzfaResponse = "";
                }
            }
            else
            {
                if (command.Length == 12 && long.TryParse(command.Substring(4), out var result))
                {
                    _shortFreq = true;
                    _slice.Freq = Math.Round(result * 1E-06, 6);
                    zzfaResponse = "";
                }
            }
        }

        return zzfaResponse;
    }
    
    
    private string Parse_ZZFB(string command)
    {
        string zzfbResponse = "?;";
        switch (command.Length)
        {
            case 4:
                if (_slice2 != null)
                {
                    long num = (long) Math.Round(_slice2.Freq * 1000000.0, 0);
                    string format = "D11";
                    if (_shortFreq)
                        format = "D8";
                    zzfbResponse = !_shortFreq || num < 100000000.0 ? "ZZFB" + num.ToString(format) + ";" : "?;";
                }
                break;
            case 12:
            case 15:
                if (long.TryParse(command.Substring(4), out var result))
                {
                    _shortFreq = command.Length == 12;
                    double vfoBFreq = Math.Round(result * 1E-06, 6);
                    _splitFreq = vfoBFreq;
                    if (_splitXit)
                    {
                        if (_slice != null)
                        {
                            _slice.XITFreq = GetXitFreq(_slice.Freq, vfoBFreq);
                            zzfbResponse = "";
                        }
                        break;
                    }
                    if (_slice2 != null)
                    {
                        _slice2.Freq = vfoBFreq;
                        zzfbResponse = "";
                    }
                }
                break;
        }
        return zzfbResponse;
    }
    
    private string Parse_ZZFI(string command)
    {
        string zzfiResponse = "?;";
        if (_slice != null)
        {
            if (command.Length == 4)
            {
                int presetFilterIndex = _slice.GetClosestPresetFilterIndex();
                switch (presetFilterIndex)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        zzfiResponse = "ZZFI" + (7 - presetFilterIndex).ToString("D2") + ";";
                        break;
                }
            }
            else
            {
                if (command.Length == 6 && int.TryParse(command.Substring(4), out var result) && result >= 0 && result <= 7)
                {
                    //_slice.SetPresetFilter(7 - result);
                    zzfiResponse = "";
                }
            }
        }
        return zzfiResponse;
    }

    
    private string Parse_ZZIF(string command)
    {
        string zzifResponse = "?;";
        if (command == "ZZIF" && _slice != null && ConnectedRadio != null)
        {
            string sliceFreq = ((long) Math.Round(_slice.Freq * 1000000.0, 0)).ToString("D11");
            if (sliceFreq.Length > 11)
                sliceFreq = "99999999999";
            string tuneStep = _slice.TuneStep.ToString("D4");
            if (tuneStep.Length > 4)
                tuneStep = "9999";
            string ritXitFreq = !_slice.XITOn ? _slice.RITFreq.ToString("+00000;-00000") : _slice.XITFreq.ToString("+00000;-00000");
            if (ritXitFreq.Length > 6)
                ritXitFreq = ritXitFreq.Substring(0, 1) + "99999";
            string ritOn = _slice.RITOn ? "1" : "0";
            string xitOn = _slice.XITOn ? "1" : "0";
            string mox = ConnectedRadio.Radio.Mox ? "1" : "0";
            string demodMode = _slice.DemodMode;
            if (demodMode == "CW" && ConnectedRadio != null && ConnectedRadio.Radio.CWL_Enabled)
                demodMode = "CWL";
            int zzModeNumber = DemodModeToFlexModeNumber(demodMode);
            string mode = "  ";
            if (zzModeNumber >= 0)
                mode = zzModeNumber.ToString("D2");
            string activeSlice = "0";
            if (_slice2 != null && _slice2.Active)
                activeSlice = "1";
            string transmitSlice = "0";
            if (_slice2 != null && _slice2.IsTransmitSlice)
                transmitSlice = "1";
            zzifResponse = "ZZIF" + sliceFreq + tuneStep + ritXitFreq + ritOn + xitOn + "000" + mox + mode + activeSlice + "0" + transmitSlice + "0000;";
        }
        return zzifResponse;
    }
    
    private string Parse_ZZMD(string command)
    {
        string zzmdResponse = "?;";
        if (_slice != null)
        {
            if (command.Length == 4)
            {
                string demodMode = _slice.DemodMode;
                if (demodMode == "CW" && ConnectedRadio != null && ConnectedRadio.Radio.CWL_Enabled)
                    demodMode = "CWL";
                int zzModeNumber = DemodModeToFlexModeNumber(demodMode);
                if (zzModeNumber >= 0)
                    zzmdResponse = "ZZMD" + zzModeNumber.ToString("D2") + ";";
            }
            else
            {
                if (command.Length == 6 && int.TryParse(command.Substring(4), out var result))
                {
                    string? demodMode = ZZModeNumberToDemodMode(result);
                    switch (demodMode)
                    {
                        case null:
                            return zzmdResponse;
                        case "CWL":
                            if (ConnectedRadio != null)
                                ConnectedRadio.Radio.CWL_Enabled = true;
                            demodMode = "CW";
                            break;
                        case "CW":
                        case "CWU":
                            if (ConnectedRadio != null)
                                ConnectedRadio.Radio.CWL_Enabled = false;
                            demodMode = "CW";
                            break;
                    }
                    _slice.DemodMode = demodMode;
                    zzmdResponse = "";
                }
            }
        }
        
        return zzmdResponse;
    }

    private string Parse_ZZRX(string command)
    {
        string zzrxResponse = "?;";
        if (ConnectedRadio != null)
        {
            switch (command)
            {
                case "ZZRX":
                    zzrxResponse = "ZZRX" + (1 - Convert.ToByte(ConnectedRadio.Radio.Mox)) + ";";
                    break;
                case "ZZRX1":
                    ConnectedRadio.Radio.Mox = false;
                    zzrxResponse = "";
                    break;
            }
        }
        return zzrxResponse;
    }

    #endregion

    private void EnsureTransmitSlice()
    {
        if (!_portSettings.AutoSwitchTxSlice)
            return;
        if (_split && _slice2 != null)
        {
            _slice2.IsTransmitSlice = true;
        }
        else
        {
            if (_slice == null)
                return;
            _slice.IsTransmitSlice = true;
        }
    }
    
    private int DemodModeToKenwoodModeNumber(string demod_mode)
    {
        int kwModeNumber = -1;
        switch (demod_mode)
        {
            case "AM":
            case "SAM":
                kwModeNumber = 5;
                break;
            case "CW":
                kwModeNumber = 3;
                break;
            case "DFM":
            case "DSTR":
            case "FDV":
            case "FM":
            case "NFM":
                kwModeNumber = 4;
                break;
            case "DIGL":
            case "RTTY":
                kwModeNumber = 6;
                break;
            case "DIGU":
                kwModeNumber = 9;
                break;
            case "LSB":
                kwModeNumber = 1;
                break;
            case "USB":
                kwModeNumber = 2;
                break;
        }
        return kwModeNumber;
    }

    private string? KenwoodModeNumberToDemodMode(int kw_mode_number)
    {
        string? demodMode = null;
        switch (kw_mode_number)
        {
            case 1:
                demodMode = "LSB";
                break;
            case 2:
                demodMode = "USB";
                break;
            case 3:
                demodMode = "CW";
                break;
            case 4:
                demodMode = "FM";
                break;
            case 5:
                demodMode = "AM";
                break;
            case 6:
                demodMode = "DIGL";
                break;
            case 9:
                demodMode = "DIGU";
                break;
        }
        return demodMode;
    }
    
    private int DemodModeToFlexModeNumber(string demodMode)
    {
      int zzModeNumber = -1;
      switch (demodMode)
      {
        case "AM":
          zzModeNumber = 6;
          break;
        case "CW":
        case "CWU":
          zzModeNumber = 4;
          break;
        case "CWL":
          zzModeNumber = 3;
          break;
        case "DFM":
          zzModeNumber = 12;
          break;
        case "DIGL":
          zzModeNumber = 9;
          break;
        case "DIGU":
          zzModeNumber = 7;
          break;
        case "DSTR":
          zzModeNumber = 40;
          break;
        case "FDV":
          zzModeNumber = 20;
          break;
        case "FM":
          zzModeNumber = 5;
          break;
        case "LSB":
          zzModeNumber = 0;
          break;
        case "NFM":
          zzModeNumber = 11;
          break;
        case "RTTY":
          zzModeNumber = 30;
          break;
        case "SAM":
          zzModeNumber = 10;
          break;
        case "USB":
          zzModeNumber = 1;
          break;
      }
      return zzModeNumber;
    }

    private string? ZZModeNumberToDemodMode(int zzModeNumber)
    {
      string? demodMode = null;
      switch (zzModeNumber)
      {
        case 0:
          demodMode = "LSB";
          break;
        case 1:
          demodMode = "USB";
          break;
        case 3:
          demodMode = "CWL";
          break;
        case 4:
          demodMode = "CWU";
          break;
        case 5:
          demodMode = "FM";
          break;
        case 6:
          demodMode = "AM";
          break;
        case 7:
          demodMode = "DIGU";
          break;
        case 9:
          demodMode = "DIGL";
          break;
        case 10:
          demodMode = "SAM";
          break;
        case 11:
          demodMode = "NFM";
          break;
        case 12:
          demodMode = "DFM";
          break;
        case 20:
          demodMode = "FDV";
          break;
        case 30:
          demodMode = "RTTY";
          break;
        case 40:
          demodMode = "DSTR";
          break;
      }
      return demodMode;
    }
    
    private int GetXitFreq(double vfoAFreq, double vfoBFreq) => (int) Math.Round((vfoBFreq - vfoAFreq) * 1000000.0, 0);

    private Slice? TransmitSlice
    {
        get
        {
            if (ConnectedRadio != null)
            {
                foreach (var slice in ConnectedRadio.Radio.SliceList)
                {
                    if (slice.IsTransmitSlice)
                    {
                        return slice;
                    }
                }
            }

            return null;
        }
    }

    private Slice? ActiveSlice
    {
        get
        {
            if (ConnectedRadio != null)
            {
                foreach (var slice in ConnectedRadio.Radio.SliceList)
                {
                    if (slice.Active)
                    {
                        return slice;
                    }
                }
            }

            return null;
        }
    }
    
    private Slice? VfoSlice(string vfo)
    {
        if (ConnectedRadio != null)
        {
            foreach (var slice in ConnectedRadio.Radio.SliceList)
            {
                if ("A".Equals(vfo))
                {
                    if (slice.Letter == _portSettings.VfoASliceLetter)
                    {
                        return slice;
                    }
                }
                else if ("B".Equals(vfo))
                {
                    if (slice.Letter == _portSettings.VfoBSliceLetter)
                    {
                        return slice;
                    }
                }
               
            }
        }

        return null;
    }
}