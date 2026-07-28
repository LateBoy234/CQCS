namespace ThreeNH.Instruments.Connection
{
    /// <summary>
    /// Owns the transport-independent connection lifecycle of an instrument.
    /// The connection descriptor is intentionally opaque so protocol details stay
    /// in infrastructure adapters.
    /// </summary>
    public interface IInstrumentConnection
    {
        bool IsOpen { get; }

        void Open(object connectionDescriptor = null);

        void Close();
    }
}
