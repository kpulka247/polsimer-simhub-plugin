using System;
using System.Drawing;
using System.Linq;
using BA63Driver.Base;
using BA63Driver.Interfaces;
using HidSharp;
using SimHub.Plugins.OutputPlugins.GraphicalDash.PSE;

namespace Polsimer.SimHub.Plugin
{
    /// <summary>
    /// Hardware LED driver implementation for Polsimer F74LED wheel using 64-bit HidSharp.
    /// </summary>
    public class PolsimerLedsDriver : DriverBase, IDisposable, ILedDriver, ILedDriverBase, IDriver
    {
        private const int VendorId = 5824;        // 0x16C0
        private const int ProductId = 1158;       // 0x0486
        private const byte CommandReportId = 248; // 0xF8
        private const int LedCount = 12;
        private const int BufferSize = LedCount * 3; // 36 bytes (RGB)

        private HidStream _stream;
        private byte[] _oldData;
        private readonly byte[] _reportBuffer = new byte[65];

        public PolsimerLedsDriver(HidStream stream)
        {
            _stream = stream;
            IsConnected = true;
        }

        public static PolsimerLedsDriver GetDevice()
        {
            try
            {
                var devices = DeviceList.Local.GetHidDevices(VendorId, ProductId).ToList();
                foreach (var dev in devices)
                {
                    try
                    {
                        var stream = dev.Open();
                        byte[] testPacket = new byte[65];
                        testPacket[0] = 0;
                        testPacket[1] = CommandReportId;
                        stream.Write(testPacket, 0, testPacket.Length);
                        return new PolsimerLedsDriver(stream);
                    }
                    catch
                    {
                        // Endpoint does not accept LED report, continue checking remaining endpoints
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Receives calculated LED colors directly from SimHub's LED Effects Engine and writes them to USB HID.
        /// </summary>
        public bool SendLeds(Color[] rGBLedsState, bool forceRefresh)
        {
            byte[] array = new byte[BufferSize];
            for (int i = 0; i < LedCount; i++)
            {
                Color color = (rGBLedsState != null && rGBLedsState.Length > i) ? rGBLedsState[i] : Color.Black;
                array[i * 3] = color.R;
                array[i * 3 + 1] = color.G;
                array[i * 3 + 2] = color.B;
            }

            if (!forceRefresh && _oldData != null && ByteArrayCompare(_oldData, array))
            {
                return false;
            }

            _oldData = array;

            if (IsConnected && _stream != null)
            {
                try
                {
                    _reportBuffer[0] = 0;               // Report ID
                    _reportBuffer[1] = CommandReportId; // 248 (0xF8)
                    Array.Copy(array, 0, _reportBuffer, 2, BufferSize);
                    _stream.Write(_reportBuffer, 0, _reportBuffer.Length);
                    return true;
                }
                catch
                {
                    IsConnected = false;
                    try { _stream?.Dispose(); } catch { }
                    _stream = null;
                }
            }
            return false;
        }

        private static bool ByteArrayCompare(byte[] a1, byte[] a2)
        {
            if (a1 == null || a2 == null || a1.Length != a2.Length) return false;
            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i]) return false;
            }
            return true;
        }

        public void Clear()
        {
            SendLeds(new Color[LedCount], forceRefresh: true);
        }

        public void Dispose()
        {
            IsConnected = false;
            Clear();
            _stream?.Dispose();
            _stream = null;
        }
    }

    /// <summary>
    /// SimHub generic manager adapter connecting PolsimerLedsDriver to SimHub's device architecture.
    /// </summary>
    public class PolsimerLedsManager : LedsGenericManager<PolsimerLedsDriver>
    {
        public PolsimerLedsManager() : base(5824, 1158)
        {
        }

        public override PolsimerLedsDriver GetDriver()
        {
            return PolsimerLedsDriver.GetDevice();
        }
    }
}