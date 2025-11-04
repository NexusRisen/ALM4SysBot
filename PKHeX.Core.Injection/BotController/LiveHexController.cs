using System;

namespace PKHeX.Core.Injection;

public sealed class LiveHeXController
{
    private readonly ISaveFileProvider SAV;
    public readonly IPKMView Editor;
    public PokeSysBotMini Bot;

    public LiveHeXController(ISaveFileProvider boxes, IPKMView editor, InjectorCommunicationType ict, bool useCache = false)
    {
        SAV = boxes;
        Editor = editor;
        var ValidVers = RamOffsets.GetValidVersions(boxes.SAV);
        var com = RamOffsets.GetCommunicator(SAV.SAV, ict);
        Bot = new PokeSysBotMini(ValidVers[0], com, useCache);
    }

    public void ChangeBox(int box)
    {
        if (!Bot.Connected)
            return;

        var sav = SAV.SAV;
        if ((uint)box >= sav.BoxCount)
            return;

        ReadBox(box);
    }

    public void ReadBox(int box)
    {
        var sav = SAV.SAV;
        var len = sav.BoxSlotCount * (RamOffsets.GetSlotSize(Bot.Version) + RamOffsets.GetGapSize(Bot.Version));
        var data = Bot.ReadBox(box, len);
        if (data.Length == 0)
            return;
        sav.SetBoxBinary(data, box);
        SAV.ReloadSlots();
    }

    public void WriteBox(int box)
    {
        var sav = SAV.SAV;
        for (int i = 0; i < sav.BoxSlotCount; i++)
        {
            var pkm = sav.GetBoxSlotAtIndex(box, i);
            sav.AdaptToSaveFile(pkm);
            var data = RamOffsets.WriteBoxData(Bot.Version) ? pkm.EncryptedBoxData : pkm.EncryptedPartyData;
            var slotData = PadSlotData(data);
            Bot.SendSlot(slotData, box, i);
        }
    }

    public void WriteActiveSlot(int box, int slot)
    {
        var pkm = Editor.PreparePKM();
        pkm.ResetPartyStats();
        SAV.SAV.AdaptToSaveFile(pkm);
        var data = RamOffsets.WriteBoxData(Bot.Version) ? pkm.EncryptedBoxData : pkm.EncryptedPartyData;
        var slotData = PadSlotData(data);
        Bot.SendSlot(slotData, box, slot);
    }

    public void ReadActiveSlot(int box, int slot)
    {
        var data = Bot.ReadSlot(box, slot);
        var pkm = SAV.SAV.GetDecryptedPKM(data.ToArray());
        Editor.PopulateFields(pkm);
    }

    public bool ReadOffset(ulong offset, RWMethod method = RWMethod.Heap)
    {
        var data = ReadData(offset, method);
        var pkm = SAV.SAV.GetDecryptedPKM(data.ToArray());

        // Since data might not actually exist at the user-specified offset, double check that the pkm data is valid.
        if (!pkm.ChecksumValid)
            return false;

        Editor.PopulateFields(pkm);
        return true;
    }

    private Span<byte> ReadData(ulong offset, RWMethod method)
    {
        return Bot.com is not ICommunicatorNX nx ? Bot.ReadOffset(offset) : method switch
        {
            RWMethod.Heap => Bot.ReadOffset(offset),
            RWMethod.Main => nx.ReadBytesMain(offset, Bot.SlotSize),
            RWMethod.Absolute => nx.ReadBytesAbsolute(offset, Bot.SlotSize),
            _ => Bot.ReadOffset(offset),
        };
    }

    public Span<byte> ReadRAM(ulong offset, int size) => Bot.com.ReadBytes(offset, size);

    public void WriteRAM(ulong offset, byte[] data) => Bot.com.WriteBytes(data, offset);

    private ReadOnlySpan<byte> PadSlotData(ReadOnlySpan<byte> data)
    {
        var slotSize = Bot.SlotSize;
        if (data.Length >= slotSize)
            return data;

        var padded = new byte[slotSize];
        data.CopyTo(padded);
        return padded;
    }
}
