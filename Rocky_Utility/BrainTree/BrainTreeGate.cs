﻿using Braintree;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rocky_Utility.BrainTree
{
    public class BrainTreeGate : IBrainTreeGate
    {
        public BrainTreeSetting _options { get; set; }
        private IBraintreeGateway braintreeGateway { get; set; }

        public BrainTreeGate(IOptions<BrainTreeSetting> options)
        {
            _options = options.Value;
        }
        public IBraintreeGateway CreateGateway()
        {
            return new BraintreeGateway(_options.Environment, _options.MerchantId, _options.PublicKey, _options.PrivateKey);
        }

        public IBraintreeGateway GetGateway()
        {
            if (braintreeGateway == null) 
            {
                braintreeGateway = CreateGateway();
            }
            return braintreeGateway;
        }
    }
}
