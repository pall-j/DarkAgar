﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgarIO.Commands
{
    [ProtoBuf.ProtoContract]
    class Move : Command
    {
        [ProtoBuf.ProtoIgnore]
        public override CommandType CommandType
        {
            get
            {
                return CommandType.Move;
            }
        }
    }
}
