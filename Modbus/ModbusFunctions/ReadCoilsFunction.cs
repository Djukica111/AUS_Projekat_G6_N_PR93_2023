using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read coil functions/requests.
    /// </summary>
    public class ReadCoilsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCoilsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
		public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc/>
        public override byte[] PackRequest()
        {
            ModbusReadCommandParameters parameters = this.CommandParameters as ModbusReadCommandParameters;
            byte[] request = new byte[12];

            // MBAP zaglavlje (7 bajtova)
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.Length)), 0, request, 4, 2);
            request[6] = parameters.UnitId;

            // PDU (5 bajtova): funkcijski kod (1) + startna adresa (2) + broj tacaka (2)
            request[7] = parameters.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.StartAddress)), 0, request, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.Quantity)), 0, request, 10, 2);

            return request;
        }

        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            Dictionary<Tuple<PointType, ushort>, ushort> dict = new Dictionary<Tuple<PointType, ushort>, ushort>();
            ModbusReadCommandParameters parameters = this.CommandParameters as ModbusReadCommandParameters;

            if ((response[7] & 0x80) != 0)
            {
                HandeException(response[8]);
            }
            else
            {
                ushort quantity = parameters.Quantity;
                for (int i = 0; i < quantity; i++)
                {
                    int byteIndex = 9 + (i / 8);
                    int bitIndex = i % 8;
                    ushort coilValue = (ushort)((response[byteIndex] >> bitIndex) & 0x01);
                    ushort address = (ushort)(parameters.StartAddress + i);
                    dict.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, address), coilValue);
                }
            }

            return dict;
        }
    }
}