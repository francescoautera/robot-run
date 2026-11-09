
using System;
using System.Collections.Generic;

namespace RobotRun
{
    public sealed class ProgramDraft
    {
        private readonly IReadOnlyList<CardSpec> inventory;
        private readonly CardInstruction?[] slots;

        public int SlotCount => slots.Length;
        public int CardCount { get; private set; }
        public CardInstruction? this[int slot] => slots[slot];

        internal bool CanPlay(int requiredDirection) => CardCount > 0 &&
            (requiredDirection == 0 || CardCount == 1 && slots[0]?.Direction == requiredDirection);

        public ProgramDraft(IReadOnlyList<CardSpec> inventory, int slotCount)
        {
            this.inventory = inventory;
            slots = new CardInstruction?[slotCount];
        }

        public int Available(int specIndex)
        {
            int available = inventory[specIndex].Quantity;
            foreach (var instruction in slots)
            {
                if (instruction.HasValue && instruction.Value.SpecIndex == specIndex)
                    available--;
            }

            return available;
        }

        public bool Place(int specIndex, int slot)
        {
            if (slots.Length == 0 || Available(specIndex) <= 0)
                return false;

            if (CardCount == slots.Length)
            {
                slot = slots.Length - 1;
            }
            else
            {
                slot = Math.Clamp(slot, 0, CardCount);
                Array.Copy(slots, slot, slots, slot + 1, CardCount - slot);
                CardCount++;
            }

            var spec = inventory[specIndex];
            slots[slot] = new CardInstruction(specIndex, spec.Type, spec.Value);
            return true;
        }

        public void Swap(int source, int target)
        {
            if ((uint)source < (uint)CardCount && (uint)target < (uint)CardCount)
                (slots[source], slots[target]) = (slots[target], slots[source]);
        }

        public void Move(int source, int insertionIndex)
        {
            if ((uint)source >= (uint)CardCount)
                return;

            int target = Math.Clamp(insertionIndex, 0, CardCount);
            if (target > source)
                target--;

            var card = slots[source];
            if (target < source)
                Array.Copy(slots, target, slots, target + 1, source - target);
            else if (target > source)
                Array.Copy(slots, source + 1, slots, source, target - source);

            slots[target] = card;
        }

        public void Remove(int slot)
        {
            if ((uint)slot >= (uint)CardCount)
                return;

            CardCount--;
            Array.Copy(slots, slot + 1, slots, slot, CardCount - slot);
            slots[CardCount] = null;
        }

        public void Flip(int slot)
        {
            if (slots[slot].HasValue)
                slots[slot] = slots[slot].Value.Flipped();
        }

        public CardInstruction?[] CreateSnapshot() => (CardInstruction?[])slots.Clone();
    }
}
