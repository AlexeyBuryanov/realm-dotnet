////////////////////////////////////////////////////////////////////////////
//
// Copyright 2020 Realm Inc.
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

#if TEST_WEAVER
using TestEmbeddedObject = Realms.EmbeddedObject;
using TestRealmObject = Realms.RealmObject;
#else
using TestEmbeddedObject = Realms.IEmbeddedObject;
using TestRealmObject = Realms.IRealmObject;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Realms.Tests.Database
{
    [TestFixture, Preserve(AllMembers = true)]
    public class DynamicEmbeddedTests : RealmInstanceTest
    {
        private void RunTestInAllModes(Action<Realm> test)
        {
            foreach (var isDynamic in new[] { true, false })
            {
                var config = new RealmConfiguration(Guid.NewGuid().ToString())
                {
                    Schema = new[] { typeof(DynamicTask), typeof(DynamicSubTask), typeof(CompletionReport), typeof(DynamicSubSubTask) },
                    IsDynamic = isDynamic
                };

                using var realm = GetRealm(config);
                test(realm);
            }
        }

        [Test]
        public void QueryAll_WhenNoObjects_ReturnsNothing()
        {
            RunTestInAllModes(realm =>
            {
                var tasks = realm.DynamicApi.All(nameof(DynamicTask));
                Assert.That(tasks.Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public void QueryAll_ByEmbedded_Fails()
        {
            RunTestInAllModes(realm =>
            {
                var ex = Assert.Throws<ArgumentException>(() => realm.DynamicApi.All(nameof(DynamicSubTask)))!;
                Assert.That(ex.Message, Does.Contain("The class DynamicSubTask represents an embedded object and thus cannot be queried directly."));
            });
        }

        [Test]
        public void CreateObject_WhenEmbedded_CanAssignToParent()
        {
            RunTestInAllModes(realm =>
            {
                var id = Guid.NewGuid().ToString();
                var now = DateTimeOffset.UtcNow;
                realm.Write(() =>
                {
                    var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), id);
                    parent.DynamicApi.Set(nameof(DynamicTask.Summary), "do great stuff!");

                    var report = realm.DynamicApi.CreateEmbeddedObjectForProperty(parent, nameof(DynamicTask.CompletionReport));
                    report.DynamicApi.Set(nameof(CompletionReport.CompletionDate), now);
                    parent.DynamicApi.Get<IEmbeddedObject>(nameof(DynamicTask.CompletionReport)).DynamicApi.Set(nameof(CompletionReport.Remarks), "success!");
                });

                var addedParent = realm.DynamicApi.Find(nameof(DynamicTask), id)!;

                Assert.That(addedParent, Is.Not.Null);
                Assert.That(addedParent.DynamicApi.Get<string>(nameof(DynamicTask.Summary)), Is.EqualTo("do great stuff!"));

                var addedReport = addedParent.DynamicApi.Get<IEmbeddedObject>(nameof(DynamicTask.CompletionReport));
                Assert.That(addedReport, Is.Not.Null);
                Assert.That(addedReport.DynamicApi.Get<DateTimeOffset>(nameof(CompletionReport.CompletionDate)), Is.EqualTo(now));
                Assert.That(addedReport.DynamicApi.Get<string>(nameof(CompletionReport.Remarks)), Is.EqualTo("success!"));
            });
        }

        [Test]
        public void ListAdd_WhenEmbedded_Throws()
        {
            RunTestInAllModes(realm =>
            {
                var ex = Assert.Throws<NotSupportedException>(() =>
                {
                    realm.Write(() =>
                    {
                        var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), Guid.NewGuid().ToString());
                        var subTasks = parent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));
                        subTasks.Add(new DynamicSubTask());
                    });
                })!;

                Assert.That(ex.Message, Does.Contain($"{nameof(realm.DynamicApi)}.{nameof(realm.DynamicApi.AddEmbeddedObjectToList)}"));
            });
        }

        [Test]
        public void DictionaryAdd_WhenEmbedded_Throws()
        {
            RunTestInAllModes(realm =>
            {
                var ex = Assert.Throws<NotSupportedException>(() =>
                {
                    realm.Write(() =>
                    {
                        var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), Guid.NewGuid().ToString());

                        var subTasksDictionary = parent.DynamicApi.GetDictionary<IEmbeddedObject>(nameof(DynamicTask.SubTasksDictionary));

                        subTasksDictionary.Add("foo", new DynamicSubTask());
                    });
                })!;

                Assert.That(ex.Message, Does.Contain($"{nameof(realm.DynamicApi)}.{nameof(realm.DynamicApi.AddEmbeddedObjectToDictionary)}"));
            });
        }

        [Test]
        public void ListInsert_WhenEmbedded_Throws()
        {
            RunTestInAllModes(realm =>
            {
                var ex = Assert.Throws<NotSupportedException>(() =>
                {
                    realm.Write(() =>
                    {
                        var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), Guid.NewGuid().ToString());
                        var subTasks = parent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));
                        subTasks.Insert(0, new DynamicSubTask());
                    });
                })!;

                Assert.That(ex.Message, Does.Contain($"{nameof(realm.DynamicApi)}.{nameof(realm.DynamicApi.InsertEmbeddedObjectInList)}"));
            });
        }

        [Test]
        public void ListSet_WhenEmbedded_Throws()
        {
            RunTestInAllModes(realm =>
            {
                var ex = Assert.Throws<NotSupportedException>(() =>
                {
                    realm.Write(() =>
                    {
                        var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), Guid.NewGuid().ToString());
                        var subTasks = parent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));
                        subTasks[0] = new DynamicSubTask();
                    });
                })!;

                Assert.That(ex.Message, Does.Contain($"{nameof(realm.DynamicApi)}.{nameof(realm.DynamicApi.SetEmbeddedObjectInList)}"));
            });
        }

        [Test]
        public void DictionarySet_WhenEmbedded_Throws()
        {
            RunTestInAllModes(realm =>
            {
                var ex = Assert.Throws<NotSupportedException>(() =>
                {
                    realm.Write(() =>
                    {
                        var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), Guid.NewGuid().ToString());
                        var subTasksDictionary = parent.DynamicApi.GetDictionary<IEmbeddedObject>(nameof(DynamicTask.SubTasksDictionary));
                        subTasksDictionary["foo"] = new DynamicSubTask();
                    });
                })!;

                Assert.That(ex.Message, Does.Contain($"{nameof(realm.DynamicApi)}.{nameof(realm.DynamicApi.SetEmbeddedObjectInDictionary)}"));
            });
        }

        [Test]
        public void RealmAddEmbeddedObjectToList()
        {
            RunTestInAllModes(realm =>
            {
                var id = Guid.NewGuid().ToString();

                realm.Write(() =>
                {
                    var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), id);
                    var subTasks = parent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));
                    var subTask = realm.DynamicApi.AddEmbeddedObjectToList(subTasks);
                    subTask.DynamicApi.Set(nameof(DynamicSubTask.Summary), "This is subtask level 1");
                });

                var addedParent = realm.DynamicApi.Find(nameof(DynamicTask), id)!;
                var addedSubTasks = addedParent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));
                Assert.That(addedSubTasks, Is.Not.Null);
                Assert.That(addedSubTasks.Count, Is.EqualTo(1));
                Assert.That(addedSubTasks[0].DynamicApi.Get<string>(nameof(DynamicTask.Summary)), Is.EqualTo("This is subtask level 1"));

                realm.Write(() =>
                {
                    var secondSubTask = realm.DynamicApi.AddEmbeddedObjectToList(addedSubTasks);
                    secondSubTask.DynamicApi.Set(nameof(DynamicSubTask.Summary), "This is a second subtask level 1");

                    var subSubTasks = addedSubTasks[0].DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicSubTask.SubSubTasks));
                    var secondLevelSubTask = realm.DynamicApi.AddEmbeddedObjectToList(subSubTasks);
                    secondLevelSubTask.DynamicApi.Set(nameof(DynamicSubSubTask.Summary), "This is subtask level 2");
                });

                addedSubTasks = addedParent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));
                Assert.That(addedSubTasks.Count, Is.EqualTo(2));
                Assert.That(addedSubTasks[1].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("This is a second subtask level 1"));
                Assert.That(addedSubTasks[1].DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicSubTask.SubSubTasks)).Count, Is.EqualTo(0));

                var addedSubSubTasks = addedSubTasks[0].DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicSubTask.SubSubTasks));
                Assert.That(addedSubSubTasks.Count, Is.EqualTo(1));
                Assert.That(addedSubSubTasks[0].DynamicApi.Get<string>(nameof(DynamicSubSubTask.Summary)), Is.EqualTo("This is subtask level 2"));
            });
        }

        [Test]
        public void RealmAddEmbeddedObjectToDictionary()
        {
            TestHelpers.IgnoreOnAOT("Indexing dynamic dictionaries is not supported on AOT platforms.");

            RunTestInAllModes(realm =>
            {
                var id = Guid.NewGuid().ToString();

                realm.Write(() =>
                {
                    var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), id);

                    var dict = parent.DynamicApi.GetDictionary<IEmbeddedObject>(nameof(DynamicTask.SubTasksDictionary));
                    var subTask = realm.DynamicApi.AddEmbeddedObjectToDictionary(dict, "foo");
                    subTask.DynamicApi.Set(nameof(DynamicSubTask.Summary), "This is subtask level 1");
                });

                var addedParent = realm.DynamicApi.Find(nameof(DynamicTask), id)!;
                var addedDict = addedParent.DynamicApi.GetDictionary<IEmbeddedObject>(nameof(DynamicTask.SubTasksDictionary));
                Assert.That(addedDict, Is.Not.Null);
                Assert.That(addedDict.Count, Is.EqualTo(1));
                Assert.That(addedDict["foo"].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("This is subtask level 1"));

                realm.Write(() =>
                {
                    var secondSubTask = realm.DynamicApi.AddEmbeddedObjectToDictionary(addedDict, "bar");
                    secondSubTask.DynamicApi.Set(nameof(DynamicSubTask.Summary), "This is a second subtask level 1");
                });

                Assert.That(addedDict.Count, Is.EqualTo(2));
                Assert.That(addedDict["bar"].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("This is a second subtask level 1"));

                if (TestHelpers.IsAOTTarget)
                {
                    return;
                }
            });
        }

        [Test]
        public void RealmInsertEmbeddedObjectInList()
        {
            RunTestInAllModes(realm =>
            {
                var id = Guid.NewGuid().ToString();

                realm.Write(() =>
                {
                    var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), id);

                    var list = parent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));

                    var subTask = realm.DynamicApi.InsertEmbeddedObjectInList(list, 0);
                    subTask.DynamicApi.Set(nameof(DynamicSubTask.Summary), "first");
                });

                var addedParent = realm.DynamicApi.Find(nameof(DynamicTask), id)!;
                var subTasks = addedParent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));

                Assert.That(subTasks, Is.Not.Null);
                Assert.That(subTasks.Count, Is.EqualTo(1));
                Assert.That(subTasks[0].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("first"));

                realm.Write(() =>
                {
                    var insertAt0 = realm.DynamicApi.InsertEmbeddedObjectInList(subTasks, 0);
                    insertAt0.DynamicApi.Set(nameof(DynamicSubTask.Summary), "This is now at 0");

                    subTasks[1].DynamicApi.Set(nameof(DynamicSubTask.Summary), "This is now at 1");

                    var insertAt2 = realm.DynamicApi.InsertEmbeddedObjectInList(subTasks, 2);
                    insertAt2.DynamicApi.Set(nameof(DynamicSubTask.Summary), "This is now at 2");
                });

                Assert.That(subTasks.Count, Is.EqualTo(3));
                Assert.That(subTasks[0].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("This is now at 0"));
                Assert.That(subTasks[1].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("This is now at 1"));
                Assert.That(subTasks[2].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("This is now at 2"));
            });
        }

        [Test]
        public void RealmSetEmbeddedObjectInList()
        {
            RunTestInAllModes(realm =>
            {
                var id = Guid.NewGuid().ToString();

                realm.Write(() =>
                {
                    var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), id);

                    var subTasks = parent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));

                    var task0 = realm.DynamicApi.AddEmbeddedObjectToList(subTasks);
                    var task1 = realm.DynamicApi.AddEmbeddedObjectToList(subTasks);
                    var task2 = realm.DynamicApi.AddEmbeddedObjectToList(subTasks);

                    task0.DynamicApi.Set(nameof(DynamicSubTask.Summary), "initial at 0");
                    task1.DynamicApi.Set(nameof(DynamicSubTask.Summary), "initial at 1");
                    task2.DynamicApi.Set(nameof(DynamicSubTask.Summary), "initial at 2");
                });

                var addedParent = realm.DynamicApi.Find(nameof(DynamicTask), id)!;
                var addedSubTasks = addedParent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));

                Assert.That(addedSubTasks.Count, Is.EqualTo(3));
                Assert.That(addedSubTasks[0].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("initial at 0"));
                Assert.That(addedSubTasks[1].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("initial at 1"));
                Assert.That(addedSubTasks[2].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("initial at 2"));

                realm.Write(() =>
                {
                    var newAt1 = realm.DynamicApi.SetEmbeddedObjectInList(addedSubTasks, 1);
                    newAt1.DynamicApi.Set(nameof(DynamicSubTask.Summary), "new at 1");
                });

                Assert.That(addedSubTasks.Count, Is.EqualTo(3));
                Assert.That(addedSubTasks[0].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("initial at 0"));
                Assert.That(addedSubTasks[1].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("new at 1"));
                Assert.That(addedSubTasks[2].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("initial at 2"));
            });
        }

        [Test]
        public void RealmSetEmbeddedObjectInDictionary()
        {
            RunTestInAllModes(realm =>
            {
                var id = Guid.NewGuid().ToString();

                realm.Write(() =>
                {
                    var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), id);

                    var dict = parent.DynamicApi.GetDictionary<IEmbeddedObject>(nameof(DynamicTask.SubTasksDictionary));
                    var task0 = realm.DynamicApi.AddEmbeddedObjectToDictionary(dict, "a");
                    var task1 = realm.DynamicApi.AddEmbeddedObjectToDictionary(dict, "b");
                    var task2 = realm.DynamicApi.AddEmbeddedObjectToDictionary(dict, "c");

                    task0.DynamicApi.Set(nameof(DynamicSubTask.Summary), "initial at a");
                    task1.DynamicApi.Set(nameof(DynamicSubTask.Summary), "initial at b");
                    task2.DynamicApi.Set(nameof(DynamicSubTask.Summary), "initial at c");
                });

                var addedParent = realm.DynamicApi.Find(nameof(DynamicTask), id)!;
                var subTasks = addedParent.DynamicApi.GetDictionary<IEmbeddedObject>(nameof(DynamicTask.SubTasksDictionary));
                Assert.That(subTasks.Count, Is.EqualTo(3));

                Assert.That(subTasks["a"].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("initial at a"));
                Assert.That(subTasks["b"].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("initial at b"));
                Assert.That(subTasks["c"].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("initial at c"));

                realm.Write(() =>
                {
                    var newAt1 = realm.DynamicApi.SetEmbeddedObjectInDictionary(subTasks, "b");
                    newAt1.DynamicApi.Set(nameof(DynamicSubTask.Summary), "new at b");
                });

                Assert.That(subTasks.Count, Is.EqualTo(3));
                Assert.That(subTasks["a"].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("initial at a"));
                Assert.That(subTasks["b"].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("new at b"));
                Assert.That(subTasks["c"].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("initial at c"));

                if (TestHelpers.IsAOTTarget)
                {
                    return;
                }
            });
        }

        [Test]
        public void RealmSetEmbeddedObjectInList_WhenOutOfBounds_Throws()
        {
            RunTestInAllModes(realm =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    realm.Write(() =>
                    {
                        var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), Guid.NewGuid().ToString());
                        var list = parent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));
                        realm.DynamicApi.SetEmbeddedObjectInList(list, 0);
                    });
                });

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    realm.Write(() =>
                    {
                        var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), Guid.NewGuid().ToString());
                        var list = parent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));
                        realm.DynamicApi.SetEmbeddedObjectInList(list, -1);
                    });
                });
            });
        }

        [Test]
        public void RealmAddEmbeddedObjectInDictionary_WhenDuplicateKeys_Throws()
        {
            RunTestInAllModes(realm =>
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    realm.Write(() =>
                    {
                        var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), Guid.NewGuid().ToString());
                        var dict = parent.DynamicApi.GetDictionary<IEmbeddedObject>(nameof(DynamicTask.SubTasksDictionary));
                        realm.DynamicApi.AddEmbeddedObjectToDictionary(dict, "foo");
                        realm.DynamicApi.AddEmbeddedObjectToDictionary(dict, "foo");
                    });
                }, "An item with the key 'foo' has already been added.");
            });
        }

        [Test]
        public void RealmInsertEmbeddedObjectInList_WhenOutOfBounds_Throws()
        {
            RunTestInAllModes(realm =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    realm.Write(() =>
                    {
                        var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), Guid.NewGuid().ToString());
                        var list = parent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));

                        // Insert at list.Count works like Add
                        realm.DynamicApi.InsertEmbeddedObjectInList(list, 1);
                    });
                });

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    realm.Write(() =>
                    {
                        var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), Guid.NewGuid().ToString());
                        var list = parent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));

                        realm.DynamicApi.SetEmbeddedObjectInList(list, -1);
                    });
                });
            });
        }

        [Test]
        public void List_RemoveAt()
        {
            RunTestInAllModes(realm =>
            {
                var id = Guid.NewGuid().ToString();

                realm.Write(() =>
                {
                    var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), id);
                    var list = parent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));

                    var first = realm.DynamicApi.AddEmbeddedObjectToList(list);
                    var second = realm.DynamicApi.AddEmbeddedObjectToList(list);

                    first.DynamicApi.Set(nameof(DynamicSubTask.Summary), "first");
                    second.DynamicApi.Set(nameof(DynamicSubTask.Summary), "second");
                });

                var addedParent = realm.DynamicApi.Find(nameof(DynamicTask), id)!;
                var list = addedParent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));
                Assert.That(list.Count, Is.EqualTo(2));

                realm.Write(() =>
                {
                    list.RemoveAt(0);
                });

                Assert.That(list.Count, Is.EqualTo(1));
                Assert.That(list[0].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("second"));
            });
        }

        [Test]
        public void List_Remove()
        {
            RunTestInAllModes(realm =>
            {
                var id = Guid.NewGuid().ToString();

                var second = realm.Write(() =>
                {
                    var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), id);
                    var list = parent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));
                    var first = realm.DynamicApi.AddEmbeddedObjectToList(list);
                    var second = realm.DynamicApi.AddEmbeddedObjectToList(list);

                    first.DynamicApi.Set(nameof(DynamicSubTask.Summary), "first");
                    second.DynamicApi.Set(nameof(DynamicSubTask.Summary), "second");

                    return second;
                });

                var addedParent = realm.DynamicApi.Find(nameof(DynamicTask), id)!;
                var subTasks = addedParent.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));
                Assert.That(subTasks.Count, Is.EqualTo(2));

                realm.Write(() =>
                {
                    subTasks.Remove(second);
                });

                Assert.That(subTasks.Count, Is.EqualTo(1));
                Assert.That(subTasks[0].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("first"));

                realm.Write(() =>
                {
                    subTasks.Remove(subTasks[0]);
                });

                Assert.That(subTasks.Count, Is.EqualTo(0));
            });
        }

        [Test]
        public void Dictionary_Remove()
        {
            TestHelpers.IgnoreOnAOT("Indexing dynamic dictionaries is not supported on AOT platforms.");

            RunTestInAllModes(realm =>
            {
                var id = Guid.NewGuid().ToString();

                var second = realm.Write(() =>
                {
                    var parent = realm.DynamicApi.CreateObject(nameof(DynamicTask), id);
                    var dict = parent.DynamicApi.GetDictionary<IEmbeddedObject>(nameof(DynamicTask.SubTasksDictionary));
                    var first = realm.DynamicApi.AddEmbeddedObjectToDictionary(dict, "first");
                    var second = realm.DynamicApi.AddEmbeddedObjectToDictionary(dict, "second");

                    first.DynamicApi.Set(nameof(DynamicSubTask.Summary), "first");
                    second.DynamicApi.Set(nameof(DynamicSubTask.Summary), "second");

                    return second;
                });

                var parent = realm.DynamicApi.Find(nameof(DynamicTask), id)!;
                var dict = parent.DynamicApi.GetDictionary<IEmbeddedObject>(nameof(DynamicTask.SubTasksDictionary));
                Assert.That(dict.Count, Is.EqualTo(2));

                realm.Write(() =>
                {
                    dict.Remove("second");
                });

                Assert.That(dict.Count, Is.EqualTo(1));
                Assert.That(dict["first"].DynamicApi.Get<string>(nameof(DynamicSubTask.Summary)), Is.EqualTo("first"));

                realm.Write(() =>
                {
                    dict.Remove("first");
                });

                Assert.That(dict.Count, Is.EqualTo(0));
            });
        }

        [Test]
        public void Embedded_Backlinks()
        {
            RunTestInAllModes(realm =>
            {
                var id = Guid.NewGuid().ToString();
                realm.Write(() =>
                {
                    var task = realm.DynamicApi.CreateObject(nameof(DynamicTask), id);
                    task.DynamicApi.Set(nameof(DynamicTask.Summary), "This is the task");

                    var subTasks = task.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks));
                    var subTask = realm.DynamicApi.AddEmbeddedObjectToList(subTasks);
                    subTask.DynamicApi.Set(nameof(DynamicSubTask.Summary), "This is level 1 subtask");

                    var subSubTasks = task.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubSubTasks));
                    _ = realm.DynamicApi.AddEmbeddedObjectToList(subSubTasks);

                    var subTaskSubSubTasks = subTask.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicSubTask.SubSubTasks));
                    _ = realm.DynamicApi.AddEmbeddedObjectToList(subTaskSubSubTasks);
                });

                var addedTask = realm.DynamicApi.Find(nameof(DynamicTask), id)!;
                var addedSubTask = addedTask.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubTasks))[0];
                var addedSubSubTask1 = addedTask.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicTask.SubSubTasks))[0];
                var addedSubSubTask2 = addedSubTask.DynamicApi.GetList<IEmbeddedObject>(nameof(DynamicSubTask.SubSubTasks))[0];

                var parentTask1 = addedSubSubTask1.DynamicApi.GetBacklinks(nameof(DynamicSubSubTask.ParentTask));
                var parentSubTask1 = addedSubSubTask1.DynamicApi.GetBacklinks(nameof(DynamicSubSubTask.ParentSubTask));
                Assert.That(parentTask1.Count(), Is.EqualTo(1));
                Assert.That(parentSubTask1.Count(), Is.EqualTo(0));
                Assert.That(parentTask1.Single(), Is.EqualTo(addedTask));

                var parentTask2 = addedSubSubTask2.DynamicApi.GetBacklinks(nameof(DynamicSubSubTask.ParentTask));
                var parentSubTask2 = addedSubSubTask2.DynamicApi.GetBacklinks(nameof(DynamicSubSubTask.ParentSubTask));
                Assert.That(parentTask2.Count(), Is.EqualTo(0));
                Assert.That(parentSubTask2.Count(), Is.EqualTo(1));
                Assert.That(parentSubTask2.Single(), Is.EqualTo(addedSubTask));
            });
        }

        [Test]
        public void Embedded_DynamicBacklinks()
        {
            RunTestInAllModes(realm =>
            {
                var id = Guid.NewGuid().ToString();
                realm.Write(() =>
                {
                    var task = realm.DynamicApi.CreateObject(nameof(DynamicTask), id);
                    task.DynamicApi.Set(nameof(DynamicTask.Summary), "This is the task");

                    var report = realm.DynamicApi.CreateEmbeddedObjectForProperty(task, nameof(DynamicTask.CompletionReport));
                    report.DynamicApi.Set(nameof(CompletionReport.Remarks), "ok");
                });

                var addedTask = realm.DynamicApi.Find(nameof(DynamicTask), id)!;
                var addedReport = addedTask.DynamicApi.Get<IEmbeddedObject>(nameof(DynamicTask.CompletionReport));

                var reportParents = addedReport.DynamicApi.GetBacklinksFromType(nameof(DynamicTask), nameof(CompletionReport));
                Assert.That(reportParents.Count(), Is.EqualTo(1));
                Assert.That(reportParents.Single(), Is.EqualTo(addedTask));
            });
        }
    }

    public partial class DynamicTask : TestRealmObject
    {
        [PrimaryKey]
        public string? Id { get; set; }

        public string? Summary { get; set; }

        public CompletionReport? CompletionReport { get; set; }

        public IList<DynamicSubTask> SubTasks { get; } = null!;

        public IList<DynamicSubSubTask> SubSubTasks { get; } = null!;

        public IDictionary<string, DynamicSubTask?> SubTasksDictionary { get; } = null!;
    }

    public partial class DynamicSubTask : TestEmbeddedObject
    {
        public string? Summary { get; set; }

        public CompletionReport? CompletionReport { get; set; }

        public IList<DynamicSubSubTask> SubSubTasks { get; } = null!;
    }

    public partial class DynamicSubSubTask : TestEmbeddedObject
    {
        public string? Summary { get; set; }

        // Singular because we only expect 1
        [Backlink(nameof(DynamicSubTask.SubSubTasks))]
        public IQueryable<DynamicSubTask> ParentSubTask { get; } = null!;

        [Backlink(nameof(DynamicTask.SubSubTasks))]
        public IQueryable<DynamicTask> ParentTask { get; } = null!;
    }

    public partial class CompletionReport : TestEmbeddedObject
    {
        public DateTimeOffset CompletionDate { get; set; }

        public string? Remarks { get; set; }
    }
}
