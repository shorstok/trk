using System;
using System.ComponentModel;

namespace trackvisualizer.Vm
{
    public partial class TrackVm : INotifyPropertyChanged, IEquatable<TrackVm>
    {
        public bool Equals(TrackVm other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(SourceTrackFileName, other.SourceTrackFileName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TrackVm) obj);
        }

        public override int GetHashCode()
        {
            return (SourceTrackFileName != null ? SourceTrackFileName.GetHashCode() : 0);
        }

        public static bool operator ==(TrackVm left, TrackVm right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TrackVm left, TrackVm right)
        {
            return !Equals(left, right);
        }
    }
}