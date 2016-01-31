﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitsetsNET
{
    public class ArrayContainer : Container
    {
        private const int DEFAULT_INIT_SIZE = 4;
        public const int DEFAULT_MAX_SIZE = 4096;

        public int cardinality;
        public ushort[] content;

        public ArrayContainer() : this(DEFAULT_INIT_SIZE) {}
        
        public ArrayContainer(int capacity)
        {
            this.cardinality = 0;
            this.content = new ushort[capacity];
        }

        private ArrayContainer(int cardinality, ushort[] inpContent)
        {
            this.cardinality = cardinality;
            this.content = inpContent;
        }

        public ArrayContainer(ushort[] newContent)
        {
            this.cardinality = newContent.Length;
            this.content = newContent;
        }

        public override Container add(ushort x)
        {
            int loc = Utility.unsignedBinarySearch(content, 0, cardinality, x);

            // if the location is positive, it means the number being added already existed in the
            // array, so no need to do anything.

            // if the location is negative, we did not find the value in the array. The location represents
            // the negative value of the position in the array (not the index) where we want to add the value
            if (loc < 0) {
                // Transform the ArrayContainer to a BitmapContainer
                // when cardinality = DEFAULT_MAX_SIZE
                if (cardinality >= DEFAULT_MAX_SIZE) {
                    BitsetContainer a = this.toBitsetContainer();
                    a.add(x);
                    return a;
                }
                if (cardinality >= this.content.Length)
                    increaseCapacity();

                // insertion : shift the elements > x by one position to the right
                // and put x in its appropriate place
                Array.Copy(content, -loc - 1, content, -loc, cardinality + loc + 1);
                content[-loc - 1] = x;
                ++cardinality;
            }
            return this;
        }

        //TODO: This needs to be optimized. It should increase capacity by more than just 1 each time
        public void increaseCapacity()
        {
            int currCapacity = this.content.Length;
            //TODO: Tori says this may be jank
            Array.Resize(ref this.content, currCapacity + 1);
        }

        public BitsetContainer toBitsetContainer()
        {
            BitsetContainer bc = new BitsetContainer();
            bc.loadData(this);
            return bc;
        }

        public void loadData(BitsetContainer bitsetContainer)
        {
            this.cardinality = bitsetContainer.cardinality;
            bitsetContainer.fillArray(content);
        }

        public override Container and(BitsetContainer x)
        {
            return x.and(this);
        }

        public override Container and(ArrayContainer value2)
        {
            ArrayContainer value1 = this;
            int desiredCapacity = Math.Min(value1.getCardinality(), value2.getCardinality());
            ArrayContainer answer = new ArrayContainer(desiredCapacity);
            answer.cardinality = Utility.unsignedIntersect2by2(value1.content,
                    value1.getCardinality(), value2.content,
                    value2.getCardinality(), answer.content);
            return answer;
        }

        public override Container clone()
        {
            ushort[] newContent = new ushort[this.content.Length];
            this.content.CopyTo(newContent, 0);
            return new ArrayContainer(this.cardinality, newContent);
        }

        public override bool contains(ushort x)
        {
            return Utility.unsignedBinarySearch(content, 0, cardinality, x) >= 0;
        }

        public override void fillLeastSignificant16bits(int[] x, int i, int mask)
        {
            throw new NotImplementedException();
        }

        public override int getCardinality()
        {
            return cardinality;
        }

        /// <summary>
        /// Performs an in-place intersection with a BitsetContainer.
        /// </summary>
        /// <param name="other">the BitsetContainer to intersect</param>
        public override Container iand(BitsetContainer other)
        {
            int pos = 0;
            for (int k = 0; k < cardinality; k++)
            {
                ushort v = content[k];
                if (other.contains(v))
                    content[pos++] = v;
            }
            cardinality = pos;
            return this;
        }

        /// <summary>
        /// Performs an in-place intersection with another ArrayContainer.
        /// </summary>
        /// <param name="other">the other ArrayContainer to intersect</param>
        public override Container iand(ArrayContainer other)
        {
            cardinality = Utility.unsignedIntersect2by2(content,
                getCardinality(), other.content,
                other.getCardinality(), content);
            return this;
        }

        public override bool intersects(BitsetContainer x)
        {
            throw new NotImplementedException();
        }

        public override bool intersects(ArrayContainer x)
        {
            throw new NotImplementedException();
        }

        public override Container ior(BitsetContainer x)
        {
            return x.or(this);
        }

        public override Container ior(ArrayContainer x)
        {
            return this.or(x);
        }

        public override Container or(BitsetContainer x)
        {
            return x.or((ArrayContainer) this);
        }

        public override Container or(ArrayContainer value2)
        {
            ArrayContainer value1 = this;
            int totalCardinality = value1.getCardinality() + value2.getCardinality();
            if (totalCardinality > DEFAULT_MAX_SIZE) {
                // it could be a bitmap!
                BitsetContainer bc = new BitsetContainer();
                for (int k = 0; k < value2.cardinality; ++k)
                {
                    ushort v = value2.content[k];
                    int i = v >> 6;
                    bc.bitmap[i] |= (1L << v);
                }
                for (int k = 0; k < this.cardinality; ++k)
                {
                    ushort v = this.content[k];
                    int i = v >> 6;
                    bc.bitmap[i] |= (1L << v);
                }
                bc.cardinality = 0;
                foreach (long k in bc.bitmap)
                {
                    bc.cardinality += Utility.longBitCount(k);
                }
                if (bc.cardinality <= DEFAULT_MAX_SIZE)
                    return bc.toArrayContainer();
                return bc;
            } else {
                // remains an array container
                int desiredCapacity = totalCardinality; // Math.min(BitmapContainer.MAX_CAPACITY,
                                                        // totalCardinality);
                ArrayContainer answer = new ArrayContainer(desiredCapacity);
                answer.cardinality = Utility.unsignedUnion2by2(value1.content,
                        value1.getCardinality(), value2.content,
                        value2.getCardinality(), answer.content);
                return answer;
            }
        }

        public override Container remove(ushort x)
        {
            throw new NotImplementedException();
        }

        public override ushort select(int j)
        {
            return this.content[j];
        }
        public override bool Equals(Object o) {
            if (o is ArrayContainer) {
                ArrayContainer srb = (ArrayContainer) o;
                if (srb.cardinality != this.cardinality)
                    return false;
                for (int i = 0; i < this.cardinality; ++i) {
                    if (this.content[i] != srb.content[i])
                        return false;
                }
                return true;
            } 
            return false;
        }

        /// <summary>
        /// Serialize this container in a binary format.
        /// </summary>
        /// <param name="writer">The writer to which to serialize this container.</param>
        /// <remarks>The serialization format is first the cardinality of the container as a 32-bit integer, followed by an array of the indices in this container as 16-bit integers.</remarks>
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(cardinality);
            foreach(ushort index in content)
            {
                writer.Write(index);
            }
        }

        /// <summary>
        /// Deserialize a container from binary format, as written by the Serialize method, minus the first 32 bits giving the cardinality.
        /// </summary>
        /// <param name="reader">The reader to deserialize from.</param>
        /// <returns>The first container represented by reader.</returns>
        public static ArrayContainer Deserialize(BinaryReader reader, int cardinality)
        {
            ArrayContainer container = new ArrayContainer(cardinality);

            container.cardinality = cardinality;
            for(int i = 0; i < cardinality; i++)
            {
                container.content[i] = (ushort) reader.ReadInt16();
            }

            return container;
        }

        public override IEnumerator<ushort> GetEnumerator()
        {
            return (IEnumerator<ushort>) content.GetEnumerator();
        }
    }
}
