using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MapChecker
{
    [TestClass]
    public class CollectionMapping
    {
        [TestMethod]
        public void verifies_mapping_from_source_to_destination()
        {
            var source = new List<Source>();

            QuickCheck.Operation(() => new TestCollection().MapFrom(source))        
                .ForCollectionMapping(x => x)
                .From(destination => source.MatchOn(src => src.SourceKey == destination.Key))
                .Verify(
                    (dest, src) => dest.Value1.MapsFrom(src.SourceValue1),
                    (dest, src) => dest.Value2.MapsFrom(src.SourceValue2)
                );
        }

        [TestMethod]
        public void supports_mapping_from_multiple_sources()
        {
            var source = new List<Source>();
            var source2 = new List<Source2>();

            QuickCheck.Operation(() => new TestCollection().MapFrom(source, source2))
                .ForCollectionMapping(x => x)
                .From(destination => source.MatchOn(src => src.SourceKey == destination.Key))
                .From((destination, src1) => source2.MatchOn(src => src.SourceKey == destination.Key))
                .Verify(
                    (dest, src, src2) => dest.Value1.MapsFrom(src.SourceValue1),
                    (dest, src, src2) => dest.Value2.MapsFrom(src.SourceValue2),
                    (dest, src, src2) => dest.BoolValue.MapsFrom(src2.Source2BoolValue)
                );
            
        }
    }

    public class TestCollection
    {
        public List<Destination> MapFrom(List<Source> source)
        {
            return source.Select(
                item => new Destination { Key = item.SourceKey, Value1 = item.SourceValue1, Value2 = item.SourceValue2})
                .ToList();
        }
        
        public List<Destination> MapFrom(List<Source> source, List<Source2> source2 )
        {
            return source.Select(
                item =>
                {
                    var s2Item = source2.First(i => i.SourceKey == item.SourceKey);
                    return
                        new Destination
                        {
                            Key = item.SourceKey,
                            Key2 = s2Item.Source2Key,
                            Value1 = item.SourceValue1,
                            Value2 = item.SourceValue2,
                            BoolValue = s2Item.Source2BoolValue
                        };
                })
                .ToList();
        }
    }

    public class Destination
    {
        public string Key { get; set; }
        public string Key2 { get; set; }
        public int Value1 { get; set; }
        public string Value2 { get; set; }
        public bool BoolValue { get; set; }
    }

    public class Source
    {
        public string SourceKey { get; set;  }
        public int SourceValue1 { get; set; }
        public string SourceValue2 { get; set; }
    }

    public class Source2
    {
        public string Source2Key { get; set; }
        public string SourceKey { get; set; }
        public bool Source2BoolValue { get; set; }
    }
}
