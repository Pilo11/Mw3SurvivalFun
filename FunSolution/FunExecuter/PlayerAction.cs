using System;
using System.Collections.Generic;
using System.Text;

namespace FunExecuter
{
    public class PlayerAction
    {

        public Position NewPlayerPosition { get; set; }

        public PlayerAction(Position newPlayerPosition)
        {
            NewPlayerPosition = newPlayerPosition;
        }
    }
}
