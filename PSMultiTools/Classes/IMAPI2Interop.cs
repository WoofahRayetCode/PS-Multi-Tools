using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace IMAPI2
{
    public interface IStream : System.Runtime.InteropServices.ComTypes.IStream { }

    public enum IMAPI_MEDIA_PHYSICAL_TYPE
    {
        IMAPI_MEDIA_TYPE_UNKNOWN = 0,
        IMAPI_MEDIA_TYPE_CDROM = 1,
        IMAPI_MEDIA_TYPE_CDR = 2,
        IMAPI_MEDIA_TYPE_CDRW = 3,
        IMAPI_MEDIA_TYPE_DVDROM = 4,
        IMAPI_MEDIA_TYPE_DVDRAM = 5,
        IMAPI_MEDIA_TYPE_DVDPLUSR = 6,
        IMAPI_MEDIA_TYPE_DVDPLUSRW = 7,
        IMAPI_MEDIA_TYPE_DVDPLUSR_DUALLAYER = 8,
        IMAPI_MEDIA_TYPE_DVDDASHR = 9,
        IMAPI_MEDIA_TYPE_DVDDASHRW = 10,
        IMAPI_MEDIA_TYPE_DVDDASHR_DUALLAYER = 11,
        IMAPI_MEDIA_TYPE_DISK = 12,
        IMAPI_MEDIA_TYPE_DVDPLUSRW_DUALLAYER = 13,
        IMAPI_MEDIA_TYPE_HDDVDROM = 14,
        IMAPI_MEDIA_TYPE_HDDVDR = 15,
        IMAPI_MEDIA_TYPE_HDDVDRAM = 16,
        IMAPI_MEDIA_TYPE_BDROM = 17,
        IMAPI_MEDIA_TYPE_BDR = 18,
        IMAPI_MEDIA_TYPE_BDRE = 19
    }

    public enum IMAPI_FORMAT2_DATA_WRITE_ACTION
    {
        IMAPI_FORMAT2_DATA_WRITE_ACTION_VALIDATING_MEDIA = 0,
        IMAPI_FORMAT2_DATA_WRITE_ACTION_FORMATTING_MEDIA = 1,
        IMAPI_FORMAT2_DATA_WRITE_ACTION_INITIALIZING_HARDWARE = 2,
        IMAPI_FORMAT2_DATA_WRITE_ACTION_CALIBRATING_POWER = 3,
        IMAPI_FORMAT2_DATA_WRITE_ACTION_WRITING_DATA = 4,
        IMAPI_FORMAT2_DATA_WRITE_ACTION_FINALIZATION = 5,
        IMAPI_FORMAT2_DATA_WRITE_ACTION_COMPLETED = 6,
        IMAPI_FORMAT2_DATA_WRITE_ACTION_VERIFYING = 7
    }

    public delegate void DDiscFormat2DataEvents_UpdateEventHandler(object sender, object progress);

    public interface IDiscFormat2DataEventArgs
    {
        IMAPI_FORMAT2_DATA_WRITE_ACTION CurrentAction { get; }
        int ElapsedTime { get; }
        int RemainingTime { get; }
        int TotalTime { get; }
        int StartLba { get; }
        int SectorCount { get; }
        int LastWrittenLba { get; }
        int TotalSystemBuffer { get; }
        int UsedSystemBuffer { get; }
        int FreeSystemBuffer { get; }
    }

    public class MsftDiscMaster2
    {
        public int Count => 0;
        public string this[int index] => "";
    }

    public class MsftDiscRecorder2
    {
        public void InitializeDiscRecorder(string recorder) { }
        public void AcquireExclusiveAccess(bool force, string clientName) { }
        public void ReleaseExclusiveAccess() { }
    }

    public class MsftDiscFormat2Data
    {
        public MsftDiscRecorder2? Recorder { get; set; }
        public string ClientName { get; set; } = "";
        public bool DisableConsumerDvdCompatibilityMode { get; set; }
        public IMAPI_MEDIA_PHYSICAL_TYPE CurrentPhysicalMediaType => IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_UNKNOWN;
        public bool MediaHeuristicallyBlank => true;
        public object? MultisessionInterfaces { get; set; }
        public event DDiscFormat2DataEvents_UpdateEventHandler? Update;
        public bool IsCurrentMediaSupported(MsftDiscRecorder2 recorder) => false;
        public void Write(IStream data) { }
    }

    public class MsftDiscFormat2Erase
    {
        public MsftDiscRecorder2? Recorder { get; set; }
        public string ClientName { get; set; } = "";
        public bool FullErase { get; set; }
        public void EraseMedia() { }
    }
}

namespace IMAPI2FS
{
    public enum IMAPI_MEDIA_PHYSICAL_TYPE
    {
        IMAPI_MEDIA_TYPE_UNKNOWN = 0,
        IMAPI_MEDIA_TYPE_CDROM = 1,
        IMAPI_MEDIA_TYPE_CDR = 2,
        IMAPI_MEDIA_TYPE_CDRW = 3,
        IMAPI_MEDIA_TYPE_DVDROM = 4,
        IMAPI_MEDIA_TYPE_DVDRAM = 5,
        IMAPI_MEDIA_TYPE_DVDPLUSR = 6,
        IMAPI_MEDIA_TYPE_DVDPLUSRW = 7,
        IMAPI_MEDIA_TYPE_DVDPLUSR_DUALLAYER = 8,
        IMAPI_MEDIA_TYPE_DVDDASHR = 9,
        IMAPI_MEDIA_TYPE_DVDDASHRW = 10,
        IMAPI_MEDIA_TYPE_DVDDASHR_DUALLAYER = 11,
        IMAPI_MEDIA_TYPE_DISK = 12,
        IMAPI_MEDIA_TYPE_DVDPLUSRW_DUALLAYER = 13,
        IMAPI_MEDIA_TYPE_HDDVDROM = 14,
        IMAPI_MEDIA_TYPE_HDDVDR = 15,
        IMAPI_MEDIA_TYPE_HDDVDRAM = 16,
        IMAPI_MEDIA_TYPE_BDROM = 17,
        IMAPI_MEDIA_TYPE_BDR = 18,
        IMAPI_MEDIA_TYPE_BDRE = 19
    }

    public class MsftFileSystemImage
    {
        public object? MultisessionInterfaces { get; set; }
        public long FreeMediaBlocks => 0;
        public void ChooseImageDefaultsForMediaType(IMAPI_MEDIA_PHYSICAL_TYPE value) { }
        public void ImportFileSystem() { }
    }
}
