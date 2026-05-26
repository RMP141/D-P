using System.Collections.Generic;
using ConvoyManager.Data;

namespace ConvoyManager.Combat
{
    public interface ICaptainCollection
    {
        IReadOnlyList<CaptainDataSO> Captains { get; }
        CaptainDataSO ActiveCaptain { get; }
        bool SetActiveCaptain(int id);
        bool AddCaptain(CaptainDataSO captain);
    }
}