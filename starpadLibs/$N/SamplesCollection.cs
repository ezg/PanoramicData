/**
 * The $N Multistroke Recognizer (C# version)
 *
 *	    Lisa Anthony, Ph.D.
 *		UMBC
 *		Information Systems Department
 * 		1000 Hilltop Circle
 *		Baltimore, MD 21250
 * 		lanthony@umbc.edu
 * 
 *      Jacob O. Wobbrock, Ph.D.
 * 		The Information School
 *		University of Washington
 *		Mary Gates Hall, Box 352840
 *		Seattle, WA 98195-2840
 *		wobbrock@u.washington.edu
 *
 * The Protractor enhancement was published by Yang Li and programmed
 * here by Lisa Anthony and Jacob O. Wobbrock.
 *
 *	Li, Y. (2010). Protractor: A fast and accurate gesture 
 *	  recognizer. Proceedings of the ACM Conference on Human 
 *	  Factors in Computing Systems (CHI '10). Atlanta, Georgia
 *	  (April 10-15, 2010). New York: ACM Press, pp. 2169-2172.
 *
 * This software is distributed under the "New BSD License" agreement:
 * 
 * Copyright (c) 2007-2011, Lisa Anthony and Jacob O. Wobbrock
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *    * Redistributions of source code must retain the above copyright
 *      notice, this list of conditions and the following disclaimer.
 *    * Redistributions in binary form must reproduce the above copyright
 *      notice, this list of conditions and the following disclaimer in the
 *      documentation and/or other materials provided with the distribution.
 *    * Neither the name of the University of Washington nor UMBC,
 *      nor the names of its contributors may be used to endorse or promote 
 *      products derived from this software without specific prior written
 *      permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
 * IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL Jacob O. Wobbrock OR Lisa Anthony 
 * BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
**/

// This class stores all samples in a Dictionary (which is Hashtable-like)
// data structure for easier processing of a huge corpus when all files
// are read in at once for batch testing (so you don't have to manually
// test each user independently). It also provides easy accessor methods
// for common corpus questions like number of users, categories, etc.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Recognizer.NDollar
{

    public class SamplesCollection : Dictionary<string, Dictionary<string, Category>> 
    {
       
        public SamplesCollection() : base()
        {
            // empty, no init
        }

        public ICollection Users
        {
            get
            {
                return this.Keys;
            }
        }

        public List<string> GetUsersList()
        {
            List<string> allUsers = new List<string>(this.Keys.Count);
            foreach (string u in this.Keys)
            {
                allUsers.Add(u);
            }
            return allUsers;
        }

        public bool ContainsUser(string user)
        {
            return this.ContainsKey(user);
        }

        public Category GetCategoryByUser(string user, string catName)
        {
            // returns null if user and/or catname are not valid
            if (!this.ContainsKey(user))
            {
                return null;
            }
            else if (!this[user].ContainsKey(catName))
            {
                return null;
            } 
            else return this[user][catName];
        }

        public bool RemoveSamples(string user, string catName)
        {
            // returns false if user and/or catname are not valid
            if (!this.ContainsKey(user))
            {
                return false;
            }
            else if (!this[user].ContainsKey(catName))
            {
                return false;
            }
            else
            {
                this[user].Remove(catName);
                return true;
            }
        }

        public void AddExample(Multistroke p)
        {
            string catName = Category.ParseName(p.Name);
            if (this.ContainsKey(p.User))
            {
                // if this user is in the collection, and has some samples in this category already...
                if (this[p.User].ContainsKey(catName))
                {
                    Dictionary<string, Category> forUser = this[p.User];
                    Category cat = (Category)forUser[catName];
                    cat.AddExample(p); // if the category has been made before, just add to it
                }
                else // create new category
                {
                    Dictionary<string, Category> forUser = this[p.User];
                    forUser.Add(catName, new Category(catName, p));
                }
            }
            else // create new user
            {
                Dictionary<string, Category> forUser = new Dictionary<string, Category>();
                forUser.Add(catName, new Category(catName, p));
                this.Add(p.User, forUser);
            }
        }

        // what is the minimum number of examples per category for this user?
        public int GetMinNumExamplesForUser(string user)
        {
            int minNumExamples = 9999;
            if ((this != null) && (this.ContainsKey(user)))
            {
                Dictionary<string, Category> allCats = this[user];
                foreach (KeyValuePair<string, Category> c in allCats)
                {
                    if (c.Value.NumExamples < minNumExamples)
                        minNumExamples = c.Value.NumExamples; 
                }
            }
            return minNumExamples;
        }

        // does this user have the same number of samples per category across all categories?
        public bool AreNumExamplesEqualForUser(string user)
        {
            if ((this != null) && (this.ContainsKey(user)))
            {
                Dictionary<string, Category> allCats = this[user];
                int prevNumExamples = -1;
                foreach (KeyValuePair<string, Category> c in allCats)
                {
                    if (prevNumExamples == -1)
                        prevNumExamples = c.Value.NumExamples;
                    if (c.Value.NumExamples != prevNumExamples)
                        return false;
                }
            } 
            return true;
        }

        public int MaxNumCategories() // across all users
        {
            return GetCategoriesList().Count;
        }

        public int NumCategoriesForUser(string user)
        {
            return this[user].Keys.Count;
        }

        public List<Category> GetCategoriesList()
        {
            List<Category> allCats = new List<Category>();
            foreach (KeyValuePair<string, Dictionary<string, Category>> u in this)
            {
                foreach (KeyValuePair<string, Category> d in u.Value)
                {
                    if (!allCats.Contains(d.Value))
                        allCats.Add(d.Value);
                }
            }
            return allCats;
        }

        public List<Category> GetCategories(string user)
        {
            List<Category> allCats = new List<Category>();
            foreach (KeyValuePair<string, Category> d in this[user])
            {
                if (!allCats.Contains(d.Value))
                    allCats.Add(d.Value);
            }
            return allCats;
        }

    }
}
