////////////////////////////////////////////////////////////////////////////
//
// Copyright 2016 Realm Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
#if TEST_WEAVER
using TestRealmObject = Realms.RealmObject;
#else
using TestRealmObject = Realms.IRealmObject;
#endif

namespace Realms.Tests.Database
{
    [Preserve(AllMembers = true)]
    public class DynamicRelationshipTests : RealmInstanceTest
    {
        private const string BilbosName = "Bilbo Fleabaggins";
        private const string EarlsName = "Earl Yippington III";
        private const string PipilottasName = "Pipilotta";
        private const string MaggiesName = "Maggie Mongrel";

        private void RunTestInAllModes(Action<Realm> test)
        {
            foreach (var isDynamic in new[] { true, false })
            {
                var config = new RealmConfiguration(Guid.NewGuid().ToString())
                {
                    Schema = new[] { typeof(DynamicOwner), typeof(DynamicDog) },
                    IsDynamic = isDynamic
                };

                using var realm = GetRealm(config);
                realm.Write(() =>
                {
                    var ownerTim = (IRealmObject)realm.DynamicApi.CreateObject(nameof(DynamicOwner));
                    ownerTim.DynamicApi.Set(nameof(DynamicOwner.Name), "Tim");

                    var dogBilbo = (IRealmObject)realm.DynamicApi.CreateObject(nameof(DynamicDog));
                    dogBilbo.DynamicApi.Set(nameof(DynamicDog.Name), BilbosName);
                    dogBilbo.DynamicApi.Set(nameof(DynamicDog.Color), "Black");

                    ownerTim.DynamicApi.Set(nameof(DynamicOwner.TopDog), RealmValue.Object(dogBilbo));  // set a one-one relationship
                    GetDogs(ownerTim).Add(dogBilbo);
                    GetDogsDict(ownerTim).Add(BilbosName, dogBilbo);
                    GetTagsDict(ownerTim).Add(BilbosName, "great");
                    ownerTim.DynamicApi.GetSet<IRealmObject>(nameof(DynamicOwner.DogsSet)).Add(dogBilbo);
                    ownerTim.DynamicApi.GetSet<string>(nameof(DynamicOwner.TagsSet)).Add("responsible");

                    var dogEarl = (IRealmObject)realm.DynamicApi.CreateObject(nameof(DynamicDog));
                    dogEarl.DynamicApi.Set(nameof(DynamicDog.Name), EarlsName);
                    dogEarl.DynamicApi.Set(nameof(DynamicDog.Color), "White");

                    GetDogs(ownerTim).Add(dogEarl);
                    GetDogsDict(ownerTim).Add(EarlsName, dogEarl);
                    GetTagsDict(ownerTim).Add(EarlsName, "playful");
                    ownerTim.DynamicApi.GetSet<IRealmObject>(nameof(DynamicOwner.DogsSet)).Add(dogEarl);
                    ownerTim.DynamicApi.GetSet<string>(nameof(DynamicOwner.TagsSet)).Add("coffee lover");

                    // lonely people and dogs
                    var ownerDani = realm.DynamicApi.CreateObject(nameof(DynamicOwner));
                    ownerDani.DynamicApi.Set(nameof(DynamicOwner.Name), "Dani");  // the dog-less

                    var dogMaggie = realm.DynamicApi.CreateObject(nameof(DynamicDog));  // will remain unassigned
                    dogMaggie.DynamicApi.Set(nameof(DynamicDog.Name), MaggiesName);
                    dogMaggie.DynamicApi.Set(nameof(DynamicDog.Color), "Grey");
                });

                test(realm);
            }
        }

        private static IRealmObject FindOwner(Realm realm, string name = "Tim")
            => realm.DynamicApi.All(nameof(DynamicOwner)).ToArray().Single(o => o.DynamicApi.Get<string>(nameof(DynamicOwner.Name)) == name);

        private static IRealmObject FindDog(Realm realm, string name = MaggiesName)
            => realm.DynamicApi.All(nameof(DynamicDog)).ToArray().Single(o => o.DynamicApi.Get<string>(nameof(DynamicDog.Name)) == name);

        private static IList<IRealmObject> GetDogs(IRealmObject owner)
            => owner.DynamicApi.GetList<IRealmObject>(nameof(DynamicOwner.Dogs));

        private static IDictionary<string, IRealmObject?> GetDogsDict(IRealmObject owner)
            => owner.DynamicApi.GetDictionary<IRealmObject?>(nameof(DynamicOwner.DogsDictionary));

        private static ISet<IRealmObject> GetDogsSet(IRealmObject owner)
            => owner.DynamicApi.GetSet<IRealmObject>(nameof(DynamicOwner.DogsSet));

        private static IList<string> GetTags(IRealmObject owner)
            => owner.DynamicApi.GetList<string>(nameof(DynamicOwner.Tags));

        private static IDictionary<string, string?> GetTagsDict(IRealmObject owner)
            => owner.DynamicApi.GetDictionary<string?>(nameof(DynamicOwner.TagsDictionary));

        private static ISet<string> GetTagsSet(IRealmObject owner)
            => owner.DynamicApi.GetSet<string>(nameof(DynamicOwner.TagsSet));

        [Test]
        public void TimHasATopDog()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                Assert.That(tim.DynamicApi.Get<IRealmObject>(nameof(DynamicOwner.TopDog)).DynamicApi.Get<string>(nameof(DynamicDog.Name)), Is.EqualTo(BilbosName));
            });
        }

        [Test]
        public void TimHasTwoIterableDogs()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                var dogNames = new List<string>();

                //// using foreach here is deliberately testing that syntax
                foreach (var dog in GetDogs(tim))
                {
                    dogNames.Add(dog.DynamicApi.Get<string>(nameof(DynamicDog.Name)));
                }

                Assert.That(dogNames, Is.EquivalentTo(new[] { BilbosName, EarlsName }));
            });
        }

        [Test]
        public void TimHasTwoIterableDogsListed()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                var dogNames = new List<string>();
                var dogList = GetDogs(tim).ToList();  // this used to crash - issue 299
                foreach (var dog in dogList)
                {
                    dogNames.Add(dog.DynamicApi.Get<string>(nameof(DynamicDog.Name)));
                }

                Assert.That(dogNames, Is.EquivalentTo(new[] { BilbosName, EarlsName }));
            });
        }

        [Test]
        public void TimRetiredHisTopDog()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);

                realm.Write(() =>
                {
                    tim.DynamicApi.Set(nameof(DynamicOwner.TopDog), RealmValue.Null);
                });

                var tim2 = FindOwner(realm);
                Assert.That(tim2.DynamicApi.Get<IRealmObject>(nameof(DynamicOwner.TopDog)), Is.Null);  // the dog departure was saved
            });
        }

        [Test]
        public void TimAddsADogLater()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                Assert.That(GetDogs(tim).Count, Is.EqualTo(2));

                realm.Write(() =>
                {
                    var maggie = FindDog(realm);
                    GetDogs(tim).Add(maggie);
                });

                var tim2 = FindOwner(realm);
                Assert.That(GetDogs(tim2).Count, Is.EqualTo(3));
                Assert.That(GetDogs(tim2)[2].DynamicApi.Get<string>(nameof(DynamicDog.Name)), Is.EqualTo(MaggiesName));
            });
        }

        [Test]
        public void TimAddsADogByInsert()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                Assert.That(GetDogs(tim).Count, Is.EqualTo(2));

                realm.Write(() =>
                {
                    var maggie = FindDog(realm);
                    GetDogs(tim).Insert(1, maggie);
                });

                var tim2 = FindOwner(realm);
                Assert.That(GetDogs(tim2).Count, Is.EqualTo(3));
                Assert.That(GetDogs(tim2)[1].DynamicApi.Get<string>(nameof(DynamicDog.Name)), Is.EqualTo(MaggiesName));
                Assert.That(GetDogs(tim2)[2].DynamicApi.Get<string>(nameof(DynamicDog.Name)), Is.EqualTo(EarlsName));
            });
        }

        [Test]
        public void TimLosesHisDogsByOrder()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                var timsDogs = GetDogs(tim);
                Assert.That(timsDogs.Count, Is.EqualTo(2));

                realm.Write(() =>
                {
                    timsDogs.RemoveAt(0);
                });

                var tim2 = FindOwner(realm);
                Assert.That(timsDogs.Count, Is.EqualTo(1));
                Assert.That(GetDogs(tim2).Count, Is.EqualTo(1));
                Assert.That(GetDogs(tim2)[0].DynamicApi.Get<string>(nameof(DynamicDog.Name)), Is.EqualTo(EarlsName));

                realm.Write(() =>
                {
                    timsDogs.RemoveAt(0);
                });

                var tim3 = FindOwner(realm);
                Assert.That(timsDogs.Count, Is.EqualTo(0));
                Assert.That(GetDogs(tim2).Count, Is.EqualTo(0));
                Assert.That(GetDogs(tim3).Count, Is.EqualTo(0)); // reloaded object has same empty related set
            });
        }

        [Test]
        public void TimLosesHisDogsInOneClear()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                var timsDogs = GetDogs(tim);
                Assert.That(timsDogs.Count, Is.EqualTo(2));
                realm.Write(() =>
                {
                    timsDogs.Clear();
                });

                var tim2 = FindOwner(realm);
                Assert.That(timsDogs.Count, Is.EqualTo(0));
                Assert.That(GetDogs(tim2).Count, Is.EqualTo(0));
            });
        }

        [Test]
        public void TimLosesBilbo()
        {
            RunTestInAllModes(realm =>
            {
                var bilbo = FindDog(realm, BilbosName);
                var tim = FindOwner(realm);
                var timsDogs = GetDogs(tim);
                Assert.That(timsDogs.Count, Is.EqualTo(2));

                realm.Write(() =>
                {
                    timsDogs.Remove(bilbo);
                });

                var tim2 = FindOwner(realm);
                Assert.That(timsDogs.Count, Is.EqualTo(1));
                Assert.That(timsDogs[0].DynamicApi.Get<string>(nameof(DynamicDog.Name)), Is.EqualTo(EarlsName));
                Assert.That(GetDogs(tim2).Count, Is.EqualTo(1));
                Assert.That(GetDogs(tim2)[0].DynamicApi.Get<string>(nameof(DynamicDog.Name)), Is.EqualTo(EarlsName));
            });
        }

        [Test]
        public void DaniHasNoTopDog()
        {
            RunTestInAllModes(realm =>
            {
                var dani = FindOwner(realm, "Dani");
                Assert.That(dani.DynamicApi.Get<IRealmObject>(nameof(DynamicOwner.TopDog)), Is.Null);
            });
        }

        [Test]
        public void DaniHasNoDogs()
        {
            RunTestInAllModes(realm =>
            {
                var dani = FindOwner(realm, "Dani");
                Assert.That(GetDogs(dani).Count, Is.EqualTo(0));
                var dogsIterated = 0;
                foreach (var d in GetDogs(dani))
                {
                    dogsIterated++;
                }

                Assert.That(dogsIterated, Is.EqualTo(0));
            });
        }

        [Test]
        public void TestExceptionsFromEmptyListOutOfRange()
        {
            RunTestInAllModes(realm =>
            {
                var dani = FindOwner(realm, "Dani");
                Assert.Throws<ArgumentOutOfRangeException>(() => GetDogs(dani).RemoveAt(0));

                var bilbo = FindDog(realm, BilbosName);
                Assert.Throws<ArgumentOutOfRangeException>(() => GetDogs(dani).Insert(-1, bilbo));
                Assert.Throws<ArgumentOutOfRangeException>(() => GetDogs(dani).Insert(1, bilbo));
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = GetDogs(dani)[0]);
            });
        }

        [Test]
        public void TestExceptionsFromIteratingEmptyList()
        {
            RunTestInAllModes(realm =>
            {
                var dani = FindOwner(realm, "Dani");
                var iter = GetDogs(dani).GetEnumerator();
                Assert.IsNotNull(iter);

                var movedOnToFirstItem = iter.MoveNext();
                Assert.That(movedOnToFirstItem, Is.False);

                Assert.Throws<ArgumentOutOfRangeException>(() => _ = iter.Current);
            });
        }

        [Test]
        public void TestExceptionsFromTimsDogsOutOfRange()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                var timsDogs = GetDogs(tim);
                Assert.Throws<ArgumentOutOfRangeException>(() => timsDogs.RemoveAt(4));
                var bilbo = FindDog(realm, BilbosName);

                Assert.Throws<ArgumentOutOfRangeException>(() => timsDogs.Insert(-1, bilbo));
                Assert.Throws<ArgumentOutOfRangeException>(() => timsDogs.Insert(3, bilbo));
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = timsDogs[99]);
            });
        }

        [Test]
        public void Backlinks()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                foreach (var dog in GetDogs(tim))
                {
                    var owners = dog.DynamicApi.GetBacklinks(nameof(DynamicDog.Owners));
                    Assert.That(owners, Is.EquivalentTo(new[] { tim }));
                }

                var dani = FindOwner(realm, "Dani");
                var maggie = FindDog(realm);
                var maggiesOwners = maggie.DynamicApi.GetBacklinks(nameof(DynamicDog.Owners));
                Assert.That(maggiesOwners, Is.Empty);

                realm.Write(() =>
                {
                    GetDogs(dani).Add(maggie);
                });

                Assert.That(maggiesOwners, Is.EquivalentTo(new[] { dani }));
            });
        }

        [Test]
        public void DynamicBacklinks()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                var topOwners = tim.DynamicApi.Get<IRealmObject>(nameof(DynamicOwner.TopDog)).DynamicApi.GetBacklinksFromType(nameof(DynamicOwner), nameof(DynamicOwner.TopDog));

                Assert.That(topOwners, Is.EquivalentTo(new[] { tim }));

                var dani = FindOwner(realm, "Dani");
                var maggie = FindDog(realm);

                Assert.That(maggie.DynamicApi.GetBacklinksFromType(nameof(DynamicOwner), nameof(DynamicOwner.TopDog)), Is.Empty);

                realm.Write(() =>
                {
                    dani.DynamicApi.Set(nameof(DynamicOwner.TopDog), RealmValue.Object(maggie));
                });

                Assert.That(maggie.DynamicApi.GetBacklinksFromType(nameof(DynamicOwner), nameof(DynamicOwner.TopDog)), Is.EquivalentTo(new[] { dani }));
            });
        }

        [Test]
        public void PrimitiveList()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);

                var timsTags = GetTags(tim);
                Assert.That(timsTags.Count, Is.EqualTo(0));

                realm.Write(() =>
                {
                    timsTags.Add("First");
                });

                Assert.That(timsTags.Count, Is.EqualTo(1));
                Assert.That(timsTags[0], Is.EqualTo("First"));
                Assert.That(timsTags.First(), Is.EqualTo("First"));

                realm.Write(() =>
                {
                    timsTags.Clear();
                });

                Assert.That(timsTags, Is.Empty);
            });
        }

        [Test]
        public void Dictionary_TryGetValue()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);

                var timsDogs = GetDogsDict(tim);
                var hasBilbo = timsDogs.TryGetValue(BilbosName, out var bilbo);
                Assert.That(hasBilbo, Is.True);
                Assert.That(bilbo, Is.Not.Null);

                var hasPipi = timsDogs.TryGetValue(PipilottasName, out var pipi);
                Assert.That(hasPipi, Is.False);
                Assert.That(pipi, Is.Null);

                var timsTags = GetTagsDict(tim);
                var hasBilbosTag = timsTags.TryGetValue(BilbosName, out var bilbosTag);
                Assert.That(hasBilbosTag, Is.True);
                Assert.That(bilbosTag, Is.EqualTo("great"));

                var hasPipisTag = timsTags.TryGetValue(PipilottasName, out var pipisTag);
                Assert.That(hasPipisTag, Is.False);
                Assert.That(pipisTag, Is.Null);
            });
        }

        [Test]
        public void Dictionary_Count()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);

                Assert.That(GetDogsDict(tim).Count, Is.EqualTo(2));
                Assert.That(GetTagsDict(tim).Count, Is.EqualTo(2));
            });
        }

        [Test]
        public void Dictionary_Get()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);

                var bilbo = GetDogsDict(tim)[BilbosName]!;
                Assert.That(bilbo, Is.Not.Null);
                Assert.That(bilbo.DynamicApi.Get<string>(nameof(DynamicDog.Name)), Is.EqualTo(BilbosName));

                var bilbosTag = GetTagsDict(tim)[BilbosName];
                Assert.That(bilbosTag, Is.EqualTo("great"));

                Assert.Throws<KeyNotFoundException>(() => _ = GetDogsDict(tim)[PipilottasName]);
                Assert.Throws<KeyNotFoundException>(() => _ = GetTagsDict(tim)[PipilottasName]);
            });
        }

        [Test]
        public void Dictionary_Set()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);

                realm.Write(() =>
                {
                    var pipi = (IRealmObject)realm.DynamicApi.CreateObject(nameof(DynamicDog));
                    pipi.DynamicApi.Set(nameof(DynamicDog.Name), PipilottasName);
                    pipi.DynamicApi.Set(nameof(DynamicDog.Color), "Orange");

                    GetDogsDict(tim)[PipilottasName] = pipi;
                    GetTagsDict(tim)[PipilottasName] = "cheerful";
                });

                Assert.That(GetDogsDict(tim).Count, Is.EqualTo(3));
                Assert.That(GetDogsDict(tim)[PipilottasName]!.DynamicApi.Get<string>(nameof(DynamicDog.Name)), Is.EqualTo(PipilottasName));

                Assert.That(GetTagsDict(tim).Count, Is.EqualTo(3));
                Assert.That(GetTagsDict(tim)[PipilottasName], Is.EqualTo("cheerful"));

                realm.Write(() =>
                {
                    GetDogsDict(tim)[PipilottasName] = null;
                    GetTagsDict(tim)[PipilottasName] = null;
                });

                Assert.That(GetDogsDict(tim).Count, Is.EqualTo(3));
                Assert.That(GetDogsDict(tim)[PipilottasName], Is.Null);

                Assert.That(GetTagsDict(tim).Count, Is.EqualTo(3));
                Assert.That(GetTagsDict(tim)[PipilottasName], Is.Null);
            });
        }

        [Test]
        public void Dictionary_Add()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);

                realm.Write(() =>
                {
                    var pipi = (IRealmObject)realm.DynamicApi.CreateObject(nameof(DynamicDog));
                    pipi.DynamicApi.Set(nameof(DynamicDog.Name), PipilottasName);
                    pipi.DynamicApi.Set(nameof(DynamicDog.Color), "Orange");

                    GetDogsDict(tim).Add(PipilottasName, pipi);
                    GetTagsDict(tim).Add(PipilottasName, "cheerful");
                });

                Assert.That(GetDogsDict(tim).Count, Is.EqualTo(3));
                Assert.That(GetDogsDict(tim)[PipilottasName]!.DynamicApi.Get<string>(nameof(DynamicDog.Name)), Is.EqualTo(PipilottasName));

                Assert.That(GetTagsDict(tim).Count, Is.EqualTo(3));
                Assert.That(GetTagsDict(tim)[PipilottasName], Is.EqualTo("cheerful"));

                Assert.Throws<ArgumentException>(() =>
                {
                    realm.Write(() =>
                    {
                        GetDogsDict(tim).Add(PipilottasName, null);
                    });
                }, $"An item with the key 'Pipilotta' has already been added.");

                Assert.Throws<ArgumentException>(() =>
                {
                    realm.Write(() =>
                    {
                        GetTagsDict(tim).Add(PipilottasName, null);
                    });
                }, $"An item with the key 'Pipilotta' has already been added.");

                Assert.That(GetDogsDict(tim).Count, Is.EqualTo(3));
                Assert.That(GetDogsDict(tim)[PipilottasName], Is.Not.Null);

                Assert.That(GetTagsDict(tim).Count, Is.EqualTo(3));
                Assert.That(GetTagsDict(tim)[PipilottasName], Is.EqualTo("cheerful"));

                realm.Write(() =>
                {
                    GetDogsDict(tim).Add("Void", null);
                    GetTagsDict(tim).Add("Void", null);
                });

                Assert.That(GetDogsDict(tim).Count, Is.EqualTo(4));
                Assert.That(GetDogsDict(tim)["Void"], Is.Null);

                Assert.That(GetTagsDict(tim).Count, Is.EqualTo(4));
                Assert.That(GetTagsDict(tim)["Void"], Is.Null);
            });
        }

        [Test]
        public void Dictionary_Keys()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                Assert.That(GetDogsDict(tim).Keys, Is.EquivalentTo(new[] { BilbosName, EarlsName }));
                Assert.That(GetTagsDict(tim).Keys, Is.EquivalentTo(new[] { BilbosName, EarlsName }));
            });
        }

        [Test]
        public void Dictionary_Values()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);

                var dogNames = new List<string>();

                foreach (var dog in GetDogsDict(tim).Values)
                {
                    dogNames.Add(dog!.DynamicApi.Get<string>(nameof(DynamicDog.Name)));
                }

                Assert.That(dogNames, Is.EquivalentTo(new[] { BilbosName, EarlsName }));
                Assert.That(GetTagsDict(tim).Values, Is.EquivalentTo(new[] { "great", "playful" }));
            });
        }

        [Test]
        public void Dictionary_Iteration()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                var dogNames = new List<string>();
                var dogColors = new List<string>();

                foreach (var kvp in GetDogsDict(tim))
                {
                    dogNames.Add(kvp.Key);
                    dogColors.Add(kvp.Value!.DynamicApi.Get<string>(nameof(DynamicDog.Color)));
                }

                Assert.That(dogNames, Is.EquivalentTo(new[] { BilbosName, EarlsName }));
                Assert.That(dogColors, Is.EquivalentTo(new[] { "Black", "White" }));

                var tags = new List<string>();
                dogNames.Clear();

                foreach (var kvp in GetTagsDict(tim))
                {
                    dogNames.Add(kvp.Key);
                    tags.Add(kvp.Value!);
                }

                Assert.That(dogNames, Is.EquivalentTo(new[] { BilbosName, EarlsName }));
                Assert.That(tags, Is.EquivalentTo(new[] { "great", "playful" }));
            });
        }

        [Test]
        public void Dictionary_Remove()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);

                Assert.That(GetDogsDict(tim).Count, Is.EqualTo(2));

                realm.Write(() =>
                {
                    GetDogsDict(tim).Remove(BilbosName);
                    GetTagsDict(tim).Remove(BilbosName);
                });

                Assert.That(GetDogsDict(tim).Count, Is.EqualTo(1));
                Assert.That(GetTagsDict(tim).Count, Is.EqualTo(1));
            });
        }

        [Test]
        public void Set_Count()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);

                Assert.That(GetDogsSet(tim).Count, Is.EqualTo(2));
                Assert.That(GetTagsSet(tim).Count, Is.EqualTo(2));
            });
        }

        [Test]
        public void Set_Add()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);

                var pipi = realm.Write(() =>
                {
                    var innerPipi = (IRealmObject)realm.DynamicApi.CreateObject(nameof(DynamicDog));
                    innerPipi.DynamicApi.Set(nameof(DynamicDog.Name), PipilottasName);
                    innerPipi.DynamicApi.Set(nameof(DynamicDog.Color), "Orange");

                    Assert.That(GetDogsSet(tim).Add(innerPipi), Is.True);
                    Assert.That(GetTagsSet(tim).Add("cheerful"), Is.True);

                    return innerPipi;
                });

                Assert.That(GetDogsSet(tim).Count, Is.EqualTo(3));
                Assert.That(GetDogsSet(tim).Any(d => d.DynamicApi.Get<string>(nameof(DynamicDog.Name)) == PipilottasName));
                Assert.That(GetDogsSet(tim).Contains(pipi));

                Assert.That(GetTagsSet(tim).Count, Is.EqualTo(3));
                Assert.That(GetTagsSet(tim).Any(t => t == "cheerful"));
                Assert.That(GetTagsSet(tim).Contains("cheerful"));

                realm.Write(() =>
                {
                    // These are already added to the sets
                    Assert.That(GetDogsSet(tim).Add(pipi), Is.False);
                    Assert.That(GetTagsSet(tim).Add("cheerful"), Is.False);
                });

                Assert.That(GetDogsSet(tim).Count, Is.EqualTo(3));
                Assert.That(GetDogsSet(tim).Any(d => d.DynamicApi.Get<string>(nameof(DynamicDog.Name)) == PipilottasName));
                Assert.That(GetDogsSet(tim).Contains(pipi));

                Assert.That(GetTagsSet(tim).Count, Is.EqualTo(3));
                Assert.That(GetTagsSet(tim).Any(t => t == "cheerful"));
                Assert.That(GetTagsSet(tim).Contains("cheerful"));
            });
        }

        [Test]
        public void Set_Iteration()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);
                var dogNames = new List<string>();
                var dogColors = new List<string>();

                foreach (var dog in GetDogsSet(tim))
                {
                    dogNames.Add(dog.DynamicApi.Get<string>(nameof(DynamicDog.Name)));
                    dogColors.Add(dog.DynamicApi.Get<string>(nameof(DynamicDog.Color)));
                }

                Assert.That(dogNames, Is.EquivalentTo(new[] { BilbosName, EarlsName }));
                Assert.That(dogColors, Is.EquivalentTo(new[] { "Black", "White" }));

                var tags = new List<string>();

                foreach (var tag in GetTagsSet(tim))
                {
                    tags.Add(tag);
                }

                Assert.That(tags, Is.EquivalentTo(new[] { "coffee lover", "responsible" }));
            });
        }

        [Test]
        public void Set_Remove()
        {
            RunTestInAllModes(realm =>
            {
                var tim = FindOwner(realm);

                Assert.That(GetDogsSet(tim).Count, Is.EqualTo(2));

                var bilbo = FindDog(realm, BilbosName);
                realm.Write(() =>
                {
                    GetDogsSet(tim).Remove(bilbo);
                    GetTagsSet(tim).Remove("responsible");
                });

                Assert.That(GetDogsSet(tim).Count, Is.EqualTo(1));
                Assert.That(GetDogsSet(tim).Contains(bilbo), Is.False);

                Assert.That(GetTagsSet(tim).Count, Is.EqualTo(1));
                Assert.That(GetTagsSet(tim).Contains("responsible"), Is.False);
            });
        }

        // Suggested https://stackoverflow.com/a/10021436/1649102
        private static TValue? TryGetValue<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey value, out bool found)
            where TKey : notnull
        {
            found = dict.TryGetValue(value, out var result);
            return result;
        }
    }

    public partial class DynamicDog : TestRealmObject
    {
        public string? Name { get; set; }

        public string? Color { get; set; }

        public bool Vaccinated { get; set; }

        [Backlink(nameof(DynamicOwner.Dogs))]
        public IQueryable<DynamicOwner> Owners { get; } = null!;
    }

    public partial class DynamicOwner : TestRealmObject
    {
        public string? Name { get; set; }

        public DynamicDog? TopDog { get; set; }

        public IList<DynamicDog> Dogs { get; } = null!;

        public IList<string> Tags { get; } = null!;

        public IDictionary<string, DynamicDog?> DogsDictionary { get; } = null!;

        public IDictionary<string, string?> TagsDictionary { get; } = null!;

        public ISet<DynamicDog> DogsSet { get; } = null!;

        public ISet<string?> TagsSet { get; } = null!;
    }
}
