using System;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Services
{
    /// <summary>Implémentation immuable de <see cref="IFaceDownBlindView"/>.</summary>
    public sealed class FaceDownBlindView : IFaceDownBlindView
    {
        public int SelfFaceDownCount { get; }

        public FaceDownBlindView(int selfFaceDownCount)
        {
            if (selfFaceDownCount < 0)
                throw new ArgumentOutOfRangeException(nameof(selfFaceDownCount));
            SelfFaceDownCount = selfFaceDownCount;
        }
    }
}