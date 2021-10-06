﻿using DOL.Database;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS {
    public static class AtlasROGManager {

        public static void GenerateROG(GameLiving living)
        {
            GenerateROG(living, false);
        }

        public static void GenerateROG(GameLiving living, bool UseEventColors)
        {
            if (living != null && living is GamePlayer)
            {
                GamePlayer player = living as GamePlayer;
                eRealm realm = player.Realm;
                eCharacterClass charclass = (eCharacterClass)player.CharacterClass.ID;

                GeneratedUniqueItem item = null;
                item = new GeneratedUniqueItem(realm, charclass, (byte)(player.Level + 1 ));
                item.AllowAdd = true;
                item.IsTradable = true;

                if (UseEventColors)
                {
                    eColor color = eColor.White;

                    switch (realm)
                    {
                        case eRealm.Hibernia:
                            color = eColor.Green_4;
                            break;
                        case eRealm.Albion:
                            color = eColor.Red_4;
                            break;
                        case eRealm.Midgard:
                            color = eColor.Blue_4;
                            break;
                    }

                    item.Color = (int)color;
                }
                
                GameServer.Database.AddObject(item);
                InventoryItem invitem = GameInventoryItem.Create<ItemUnique>(item);
                player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invitem);
            }
        }

    }
}
