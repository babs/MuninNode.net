using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ddegois.Net.CIDR {
	public class CIDRCollection : List<CIDR> {
		public void Add (string ip) {
			this.Add(new CIDR(ip));
		}
		public bool Has (string ip) {
			return this.Has(IPAddress.Parse(ip));
		}
		public bool Has (IPAddress ip) {
			for (int i = 0; i < this.Count; i++) {
				if (this[i].Has(ip)) {
					return true;
				}
			}
			return false;
		}
		public override string ToString () {
			if (this.Count == 0) {
				return "Empty CIDRCollection";
			}
			string[] inColCIRD = new string[this.Count];
			for (int i = 0; i < this.Count; i++) {
				inColCIRD[i] = this[i].ToString();
			}
			return String.Concat("CIDRCollection of: ",String.Join(", ", inColCIRD), ".");
		}
	}

	public class CIDR {
		private const byte byteFF = 0xff;
		public IPAddress Address {
			get {
				return _ip;
			}
		}
		public IPAddress Netmask {
			get {
				return _netmask;
			}
		}
		private IPAddress _ip = null;
		private IPAddress _netmask = null;
		private byte[] _ip_bytes = null;
		private byte[] _netmask_bytes = null;
		private int? _netmaskbitlength = null;
		public CIDR(string CIDR) {
			string[] splitted_CIDR = CIDR.Split('/');
			if (splitted_CIDR.Length > 2) {
				throw new ApplicationException("Given string is not a valid CIDR: " + CIDR);
			}
			
			_ip = IPAddress.Parse(splitted_CIDR[0]);
			_ip_bytes = _ip.GetAddressBytes();
			_netmask_bytes = new byte[_ip_bytes.Length];
			
			if (splitted_CIDR.Length == 1) {
				for (int i = 0; i < _ip_bytes.Length; i++) {
					_netmask_bytes[i] = byteFF;
				}
				_netmaskbitlength = _ip_bytes.Length * 8;
			} else {
				byte netmaskbitlength = Convert.ToByte(splitted_CIDR[1]);
				_netmaskbitlength = netmaskbitlength;
				byte currentbitlength = 0;
				for (var i = 0; i < _ip_bytes.Length; i++) {
					if (netmaskbitlength > 8) {
						currentbitlength = 8;
					} else {
						currentbitlength = netmaskbitlength;
					}
					_netmask_bytes[i] = (byte)~(byteFF >> currentbitlength);
					netmaskbitlength -= currentbitlength;
				}
			}
			_netmask = new IPAddress(_netmask_bytes);
		}
		public bool Has (string ip) {
			return this.Has(IPAddress.Parse(ip));
		}
		public bool Has (IPAddress ip) {
			byte[] given_ip = ip.GetAddressBytes();
			for (int i = 0; i < given_ip.Length; i++) {
				if (!((_netmask_bytes[i] & _ip_bytes[i]) == (_netmask_bytes[i] & given_ip[i]))) {
					return false;
				}
			}
			return true;
		}
		public override string ToString () {
			return Address + "/" + _netmaskbitlength;
		}
	}
}

