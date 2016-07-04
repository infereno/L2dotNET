﻿using System;
using System.Linq;
using L2dotNET.GameService.Model.Inventory;
using L2dotNET.GameService.Model.Player;
using L2dotNET.GameService.Network.Serverpackets;
using L2dotNET.GameService.Tables;
using L2dotNET.GameService.Tools;
using L2dotNET.GameService.World;

namespace L2dotNET.GameService.Model.Items
{
    public class L2Item : L2Object
    {
        public ItemTemplate Template;
        public int Count;
        public short IsEquipped;
        public int Enchant;
        public short Enchant1;
        public short Enchant2;
        public short Enchant3;
        public int AugmentationId = 0;
        public int Durability;
        public ItemLocation Location;
        public int PaperdollSlot = -1;
        public int PetId = -1;
        public int Dropper;
        public int SlotLocation = 0;

        public short AttrAttackType = -2;
        public short AttrAttackValue = 0;
        public short AttrDefenseValueFire = 0;
        public short AttrDefenseValueWater = 0;
        public short AttrDefenseValueWind = 0;
        public short AttrDefenseValueEarth = 0;
        public short AttrDefenseValueHoly = 0;
        public short AttrDefenseValueUnholy = 0;

        public bool Blocked = false;
        public bool TempBlock = false;

        public L2Item(ItemTemplate template)
        {
            ObjId = IdFactory.Instance.nextId();
            Template = template;
            Count = 1;
            Durability = template.Durability;
            Location = ItemLocation.Void;

            if (template.LimitedMinutes > 0)
            {
                _lifeTimeEndEnabled = true;
                _lifeTimeEndTime = DateTime.Now.AddMinutes(template.LimitedMinutes);
                //    lifeTimeEnd = DateTime.Now.AddHours(template.LimitedHours).ToString("yyyy-MM-dd HH-mm-ss");
            }

            if (template.enchanted > 0)
                Enchant = template.enchanted;
        }

        public void genId()
        {
            ObjId = IdFactory.Instance.nextId();
        }

        /** Enumeration of locations for item */
        public enum ItemLocation
        {
            Void,
            Inventory,
            Paperdoll,
            Warehouse,
            Clanwh,
            Pet,
            PetEquip,
            Lease,
            Freight
        }

        public void Unequip(L2Player owner)
        {
            IsEquipped = 0;
            PaperdollSlot = -1;

            if (Template.AbnormalMaskEvent > 0)
                owner.AbnormalBitMaskEvent &= ~Template.AbnormalMaskEvent;

            bool upsend = false;
            if (Template.item_skill != null)
            {
                upsend = true;
                owner.RemoveSkill(Template.item_skill.skill_id, false, false);
            }

            if (Template.item_skill_ench4 != null)
            {
                upsend = true;
                owner.RemoveSkill(Template.item_skill_ench4.skill_id, false, false);
            }

            if (Template.unequip_skill != null)
                owner.AddEffect(owner, Template.unequip_skill, true, false);

            Location = ItemLocation.Inventory;

            if (upsend)
                owner.UpdateSkillList();

            if ((Template.WeaponType == ItemTemplate.L2ItemWeaponType.bow) || (Template.WeaponType == ItemTemplate.L2ItemWeaponType.crossbow))
            {
                //owner.Inventory.setPaperdoll(InvPC.EQUIPITEM_LHand, null, true);
                owner.SecondaryWeaponSupport = null;
            }

            if (Template.SetItem)
                ItemTable.Instance.NotifyKeySetItem(owner, this, false);

            if ((Template.Type == ItemTemplate.L2ItemType.armor) && (owner.setKeyItems != null) && owner.setKeyItems.Contains(Template.ItemID))
                ItemTable.Instance.NotifySetItemEquip(owner, this, false);

            if ((Template.Type == ItemTemplate.L2ItemType.armor) || (Template.Type == ItemTemplate.L2ItemType.weapon) || (Template.Type == ItemTemplate.L2ItemType.accessary))
                if (Enchant == 0)
                    owner.SendPacket(new SystemMessage(SystemMessage.SystemMessageId.S1_DISARMED).AddItemName(Template.ItemID));
                else
                    owner.SendPacket(new SystemMessage(SystemMessage.SystemMessageId.EQUIPMENT_S1_S2_REMOVED).AddNumber(Enchant).AddItemName(Template.ItemID));

            owner.RemoveStats(this);
        }

        public void Equip(L2Player owner)
        {
            IsEquipped = 1;

            if (Template.AbnormalMaskEvent > 0)
                owner.AbnormalBitMaskEvent |= Template.AbnormalMaskEvent;

            bool upsend = false;
            if (Template.item_skill != null)
            {
                upsend = true;
                owner.AddSkill(Template.item_skill, false, false);
            }

            if ((Template.item_skill_ench4 != null) && (Enchant >= 4))
            {
                upsend = true;
                owner.AddSkill(Template.item_skill_ench4, false, false);
            }

            Location = ItemLocation.Paperdoll;

            if ((Template.Bodypart == ItemTemplate.L2ItemBodypart.lhand) && (Template.WeaponType == ItemTemplate.L2ItemWeaponType.shield))
            {
                //L2Item weapon = owner.Inventory.getWeapon();
                //if ((weapon != null) && (weapon.Template.Bodypart == ItemTemplate.L2ItemBodypart.lrhand))
                //    owner.Inventory.setPaperdoll(InvPC.EQUIPITEM_RHand, null, true);
            }

            if (upsend)
                owner.UpdateSkillList();

            if ((Template.WeaponType == ItemTemplate.L2ItemWeaponType.bow) || (Template.WeaponType == ItemTemplate.L2ItemWeaponType.crossbow))
                TryEquipSecondary(owner);

            if (Template.SetItem)
                ItemTable.Instance.NotifyKeySetItem(owner, this, true);

            if ((Template.Type == ItemTemplate.L2ItemType.armor) && (owner.setKeyItems != null) && owner.setKeyItems.Contains(Template.ItemID))
                ItemTable.Instance.NotifySetItemEquip(owner, this, true);

            if ((Template.Type == ItemTemplate.L2ItemType.armor) || (Template.Type == ItemTemplate.L2ItemType.weapon) || (Template.Type == ItemTemplate.L2ItemType.accessary))
                if (Enchant == 0)
                    owner.SendPacket(new SystemMessage(SystemMessage.SystemMessageId.S1_EQUIPPED).AddItemName(Template.ItemID));
                else
                    owner.SendPacket(new SystemMessage(SystemMessage.SystemMessageId.S1_S2_EQUIPPED).AddNumber(Enchant).AddItemName(Template.ItemID));

            owner.AddStats(this);
        }

        public void NotifyStats(L2Player owner)
        {
            if (Template.AbnormalMaskEvent > 0)
                owner.AbnormalBitMaskEvent |= Template.AbnormalMaskEvent;

            if (Template.item_skill != null)
                owner.AddSkill(Template.item_skill, false, false);

            if ((Template.item_skill_ench4 != null) && (Enchant >= 4))
                owner.AddSkill(Template.item_skill_ench4, false, false);

            if ((Template.WeaponType == ItemTemplate.L2ItemWeaponType.bow) || (Template.WeaponType == ItemTemplate.L2ItemWeaponType.crossbow))
                TryEquipSecondary(owner);

            if (Template.SetItem)
                ItemTable.Instance.NotifyKeySetItem(owner, this, true);

            if ((Template.Type == ItemTemplate.L2ItemType.armor) && (owner.setKeyItems != null) && owner.setKeyItems.Contains(Template.ItemID))
                ItemTable.Instance.NotifySetItemEquip(owner, this, true);

            owner.AddStats(this);
        }

        private void TryEquipSecondary(L2Player owner)
        {
            int secondaryId1,
                secondaryId2;
            bool bow = Template.WeaponType == ItemTemplate.L2ItemWeaponType.bow;
            switch (Template.CrystallGrade)
            {
                case ItemTemplate.L2ItemGrade.none:
                    secondaryId1 = bow ? 17 : 9632;
                    secondaryId2 = 0;
                    break;
                case ItemTemplate.L2ItemGrade.d:
                    secondaryId1 = bow ? 1341 : 9633;
                    secondaryId2 = bow ? 22067 : 22144;
                    break;
                case ItemTemplate.L2ItemGrade.c:
                    secondaryId1 = bow ? 1342 : 9634;
                    secondaryId2 = bow ? 22068 : 22145;
                    break;
                case ItemTemplate.L2ItemGrade.b:
                    secondaryId1 = bow ? 1343 : 9635;
                    secondaryId2 = bow ? 22069 : 22146;
                    break;
                case ItemTemplate.L2ItemGrade.a:
                    secondaryId1 = bow ? 1344 : 9636;
                    secondaryId2 = bow ? 22070 : 22147;
                    break;
                default: //Ы+
                    secondaryId1 = bow ? 1345 : 9637;
                    secondaryId2 = bow ? 22071 : 22148;
                    break;
            }

            //foreach (L2Item sec in owner.Inventory.Items.Values.Where(sec => (sec.Template.ItemID == secondaryId1) || (sec.Template.ItemID == secondaryId2)))
            //{
            //    //owner.Inventory.setPaperdoll(InvPC.EQUIPITEM_LHand, sec, true);
            //    owner.SecondaryWeaponSupport = sec;
            //    break;
            //}
        }

        public void DropMe(int x, int y, int z, L2Character dropper, L2Character killer, int seconds)
        {
            X = x;
            Y = y;
            Z = z;
            DropItem pk = new DropItem(this);
            if (dropper != null)
                Dropper = dropper.ObjId;

            Location = ItemLocation.Void;

            killer?.AddKnownObject(this, pk, true);

            L2World.Instance.AddObject(this);
        }

        public void DropMe(int x, int y, int z)
        {
            DropMe(x, y, z, null, null, 0);
        }

        public override void OnAction(L2Player player)
        {
            double dis = Calcs.calculateDistance(this, player, true);
            player.SendMessage(AsString() + " dis " + (int)dis);
            if (dis < 80)
            {
                foreach (L2Player o in KnownObjects.Values.OfType<L2Player>())
                {
                    o.SendPacket(new GetItem(player.ObjId, ObjId, X, Y, Z));
                    o.SendPacket(new DeleteObject(ObjId));
                }

                player.OnPickUp(this);

                L2World.Instance.RemoveObject(this);
            }
            else
            {
                player.TryMoveTo(X, Y, Z);
            }
        }

        public override void OnForcedAttack(L2Player player)
        {
            player.SendActionFailed();
        }

        private bool _lifeTimeEndEnabled;
        private DateTime _lifeTimeEndTime;
        public int CustomType1;
        public int CustomType2;
        public bool Soulshot = false,
                    Spiritshot = false,
                    BlessSpiritshot = false;

        public int LifeTimeEnd()
        {
            if (!_lifeTimeEndEnabled)
                return -9999;

            TimeSpan ts = _lifeTimeEndTime - DateTime.Now;
            return (int)ts.TotalSeconds;
        }

        public void AddLimitedHour(int hours)
        {
            if (_lifeTimeEndEnabled)
            {
                _lifeTimeEndTime = _lifeTimeEndTime.AddHours(hours);
            }
            else
            {
                _lifeTimeEndEnabled = true;
                _lifeTimeEndTime = DateTime.Now.AddHours(hours);
            }
        }

        public void SetLimitedHour(string str)
        {
            if (str != "-1")
            {
                string[] x1 = str.Split(' ');
                int yy = Convert.ToInt32(x1[0].Split('-')[0]);
                int mm = Convert.ToInt32(x1[0].Split('-')[1]);
                int dd = Convert.ToInt32(x1[0].Split('-')[2]);
                int hh = Convert.ToInt32(x1[1].Split('-')[0]);
                int m = Convert.ToInt32(x1[1].Split('-')[1]);
                int ss = Convert.ToInt32(x1[1].Split('-')[2]);

                DateTime dt = new DateTime(yy, mm, dd, hh, m, ss);
                if (dt > DateTime.Now)
                {
                    _lifeTimeEndEnabled = true;
                    _lifeTimeEndTime = dt;
                }
                //TODO delete me
            }
        }

        public void sql_insert(int id)
        {
            //SQL_Block sqb = new SQL_Block("user_items");
            //sqb.param("ownerId", id);
            //sqb.param("iobjectId", ObjID);
            //sqb.param("itemId", Template.ItemID);
            //sqb.param("icount", Count);
            //sqb.param("ienchant", Enchant);
            //sqb.param("iaugment", AugmentationID);
            //sqb.param("imana", Durability);
            //sqb.param("lifetime", LimitedHourStr());
            //sqb.param("iequipped", _isEquipped);
            //sqb.param("iequip_data", _paperdollSlot);
            //sqb.param("ilocation", Location.ToString());
            //sqb.param("iloc_data", 0);
            ////sqb.param("ict1", CustomType1);
            ////sqb.param("ict2", CustomType2);
            //sqb.sql_insert(false);
        }

        public void sql_delete()
        {
            //SQL_Block sqb = new SQL_Block("user_items");
            //sqb.where("iobjectId", ObjID);
            //sqb.sql_delete(false);
        }

        public void sql_update()
        {
            //SQL_Block sqb = new SQL_Block("user_items");
            //sqb.param("itemId", Template.ItemID);
            //sqb.param("icount", Count);
            //sqb.param("ienchant", Enchant);
            //sqb.param("iaugment", AugmentationID);
            //sqb.param("imana", Durability);
            //sqb.param("lifetime", LimitedHourStr());
            //sqb.param("iequipped", _isEquipped);
            //sqb.param("iequip_data", _paperdollSlot);
            //sqb.param("ilocation", Location.ToString());
            //sqb.param("iloc_data", SlotLocation);
            ////sqb.param("ict1", CustomType1);
            ////sqb.param("ict2", CustomType2);
            //sqb.where("iobjectId", ObjID);
            //sqb.sql_update(false);
        }

        public override string AsString()
        {
            return "L2Item:" + Template.ItemID + "; count " + Count + "; enchant " + Enchant + "; id " + ObjId;
        }

        public bool NotForTrade()
        {
            return (Template.is_trade == 0) || (AugmentationId > 0) || (IsEquipped == 1);
        }

        public bool NotForSale()
        {
            return (Template.is_trade == 0) || (IsEquipped == 1);
        }
    }
}