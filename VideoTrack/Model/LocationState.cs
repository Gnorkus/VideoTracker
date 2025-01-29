using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoTrack.Model
{
    public class LocationState : CoreDatabase
    {
        [PrimaryIdentityKey] public int LocationStatePK { get; }
        [MaxLength(50)] public string LocationStateDesc { get; set; }
    }
}
