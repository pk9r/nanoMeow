// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoMeow.CanBus.Extensions;

namespace nanoMeow.CanBus
{
    public class CanButton
    {
        public bool IsActivated { get; set; }

        public uint CanId { get; set; }

        public byte BitIndex { get; set; }

        public TimeSpan ShortDebounceTime { get; set; } =
            TimeSpan.FromMilliseconds(1000);

        private DateTime _startTime;

        public event EventHandler OnPressed;

        public event EventHandler OnReleased;

        public event EventHandler OnShortClicked;

        public void Update(CanFrame frame)
        {
            if (frame.Id != CanId)
            {
                return;
            }

            bool isActivated = frame.IsBitSet(BitIndex);
            if (IsActivated != isActivated)
            {
                DateTime now = DateTime.UtcNow;

                IsActivated = isActivated;
                if (isActivated)
                {
                    _startTime = now;
                    OnPressed?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    OnReleased?.Invoke(this, EventArgs.Empty);
                    if (now - _startTime < ShortDebounceTime)
                    {
                        OnShortClicked?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }
    }
}
