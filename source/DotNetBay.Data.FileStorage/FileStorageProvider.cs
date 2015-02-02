﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetBay.Interfaces;
using DotNetBay.Model;
using Newtonsoft.Json;

namespace DotNetBay.Data.FileStorage
{
    public class FileStorageProvider : IStorageProvider
    {
        // Poor-Mans locking mechanism
        private readonly object syncRoot = new object();

        private bool isLoaded;
        private readonly string fullPath;

        private DataRootElement data;
        private readonly JsonSerializerSettings jsonSerializerSettings;

        public FileStorageProvider(string fileName)
        {
            // It's good practice to expect either absolute or relative paths and handle both the same
            this.fullPath = Path.GetFullPath(fileName);

            this.jsonSerializerSettings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };
        }

        public void Load()
        {
            if (!File.Exists(this.fullPath))
            {
                var file = File.Create(this.fullPath);
                file.Close();
            }

            var content = File.ReadAllText(this.fullPath);

            var restored = JsonConvert.DeserializeObject<DataRootElement>(content, this.jsonSerializerSettings);

            if (restored != null)
            {
                this.data = restored;
            }
            else
            {
                this.data = new DataRootElement();
            }

            this.isLoaded = true;
        }

        public void Save()
        {
            var content = JsonConvert.SerializeObject(this.data, this.jsonSerializerSettings);

            File.WriteAllText(this.fullPath, content);
        }

        private void EnsureCompleteLoaded()
        {
            if (!this.isLoaded || this.data == null || this.data.Auctions == null || this.data.Bids == null ||
                this.data.Members == null)
            {
                this.Load();
            }
        }

        public Auction Add(Auction auction)
        {
            if (auction.Seller == null)
            {
                throw new ArgumentException("Its required to set a seller");
            }

            lock (this.syncRoot)
            {
                this.EnsureCompleteLoaded();

                if (this.data.Auctions.Any(a => a.Id == auction.Id))
                {
                    throw new ArgumentException("The auction is already stored");
                }

                // TODO: Fail if the references are not the same

                var maxId = this.data.Auctions.Any() ? this.data.Auctions.Max(a => a.Id) : 0;

                auction.Id = maxId + 1;

                this.data.Auctions.Add(auction);

                // Find the member in "store"
                var seller = this.data.Members.FirstOrDefault(m => m.UniqueId == auction.Seller.UniqueId);

                // Create member as seller if not exists
                if (seller == null)
                {
                    // The seller does not yet exist in store
                    seller = auction.Seller;
                    seller.Auctions = new List<Auction>(new[] {auction});
                    this.data.Members.Add(seller);
                }

                // Add auction to sellers list of auctions
                if (seller.Auctions.All(a => a.Id != auction.Id))
                {
                    seller.Auctions.Add(auction);
                }

                this.Save();

                return auction;
            }
        }

        public Member Add(Member member)
        {
            lock (this.syncRoot)
            {
                this.EnsureCompleteLoaded();

                if (this.data.Members.Any(m=> m.UniqueId == member.UniqueId))
                {
                    throw new ArgumentException("A member with the same uniqueId already exists!");
                }

                // TODO: Fail if the references are not the same

                this.data.Members.Add(member);

                if (member.Auctions != null && member.Auctions.Any())
                {
                    foreach (var auction in member.Auctions)
                    {
                        this.Add(auction);
                    }
                }

                this.Save();

                return member;
            }
        }

        public Auction Update(Auction auction)
        {
            if (auction.Seller == null)
            {
                throw new ArgumentException("Its required to set a seller");
            }

            lock (this.syncRoot)
            {
                this.EnsureCompleteLoaded();

                if (this.data.Auctions.All(a => a.Id != auction.Id))
                {
                    throw new ApplicationException("This auction does not exist and cannot be updated!");
                }

                // TODO: Fail if the references are not the same

                // TODO Update 

                // TODO Handle Bids (references, add to other list)

                return null;
            }
        }

        public Bid Add(Bid bid)
        {
            if (bid.Bidder == null)
            {
                throw new ArgumentException("Its required to set a bidder");
            }

            if (bid.Auction == null)
            {
                throw new ArgumentException("Its required to set an auction");
            }

            lock (this.syncRoot)
            {
                this.EnsureCompleteLoaded();

                // Does the auction exist?
                if (this.data.Auctions.All(a => a.Id != bid.Auction.Id))
                {
                    throw new ApplicationException("This auction does not exist an cannot be added this way!");
                }

                // Does the member exist?
                if (this.data.Members.All(a => a.UniqueId != bid.Bidder.UniqueId))
                {
                    throw new ApplicationException("the bidder does not exist and cannot be added this way!");
                }

                // TODO: Fail if the references are not the same

                var maxId = this.data.Bids.Any() ? this.data.Bids.Max(a => a.Id) : 0;
                bid.Id = maxId + 1;
                bid.Accepted = null;
                bid.TransactionId = Guid.NewGuid();
                
                this.data.Bids.Add(bid);

                // Reference back from auction
                var auction = this.data.Auctions.FirstOrDefault(a => a.Id == bid.Auction.Id);
                auction.Bids.Add(bid);

                return null;
            }
        }

        public Bid GetBidByTransactionId(Guid transactionId)
        {
            lock (this.syncRoot)
            {
                this.EnsureCompleteLoaded();

                return this.data.Bids.FirstOrDefault(b => b.TransactionId == transactionId);
            }
        }

        public IQueryable<Auction> GetAuctions()
        {
            lock (this.syncRoot)
            {
                this.EnsureCompleteLoaded();

                return this.data.Auctions.AsQueryable();
            }
        }

        public IQueryable<Member> GetMembers()
        {
            lock (this.syncRoot)
            {
                this.EnsureCompleteLoaded();

                return this.data.Members.AsQueryable();
            }
        }
    }
}
