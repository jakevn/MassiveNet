// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;

namespace Massive.Examples.NetAdvanced {

    public interface IInvItem {
        uint DbId { get; }

        string Name { get; }

        int QuantityMax { get; }

        int Quantity { get; set; }

        IInvItem Clone(int withQuantity);

        IInvItem Clone(int withQuantity, NetStream stream);

        void WriteAdditional(NetStream stream);
    }

}
