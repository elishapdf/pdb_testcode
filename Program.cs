using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Puzzle.MicroHeap;

namespace Laboratory.AI
{

    public class Program
    {
        static void Main()
        {

            var initWorstConfig3x3 = new[,] {   {8,6,7},
                                                {2,5,4},
                                                {3,0,1}
                                    };

            var initConfig4x4 = new[,] {     {5,10,14,7},
                                             {8,3,6,1},
                                             {15,0,12,9},
                                             {2,11,4,13}
                                    };

            var finalConfig3x3 = new[,] {    {1,2,3},
                                             {4,5,6},
                                             {7,8,0}
                                    };

            var finalConfig4x4 = new[,] {    {1,2,3,4},
                                             {5,6,7,8},
                                             {9,10,11,12},
                                             {13,14,15,0}
                                    };

            var initialState = new StateNode<int>(initWorstConfig3x3, 2, 1, 0);
            var finalState = new StateNode<int>(finalConfig3x3, 2, 2, 0);    

            var watch = new Stopwatch();
            var aStar = new AStar<int>(initialState, finalState, 0)
            {
                PatternDatabase = FillPatternDatabase()
            };
                            

            watch.Start();
            var node = aStar.Execute();
            watch.Stop();
            
            Console.WriteLine("Node at depth {0}", node.Depth);
            Console.WriteLine("States visited {0}", aStar.StatesVisited);
            Console.WriteLine("Elapsed {0} miliseconds", watch.ElapsedMilliseconds);
            Console.Read();
        }

        private static Dictionary<string, int> FillPatternDatabase()
        {
            var reader = new StreamReader("F://pdb.txt");
            var database = new Dictionary<string, int>();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var pattern = line.Split('|')[0].TrimEnd(' ');

                pattern = pattern.Replace('5', '?')
                                 .Replace('6', '?')
                                 .Replace('7', '?')
                                 .Replace('8', '?');

                if (!database.ContainsKey(pattern))
                    database.Add(pattern, int.Parse(line.Split('|')[1]));
            }
            reader.Close();

            return database;
        }

        class StateNode<T>: IComparable<StateNode<T>> where T: IComparable
        {
            public double Value { get; set; }
            public T[,] State { get; private set; }
            public int EmptyCol { get; private set; }
            public int EmptyRow { get; private set; }
            public int Depth { get; set; }
            public string StrRepresentation { get; set; }

            public StateNode() { }

            public StateNode(T[,] state, int emptyRow, int emptyCol, int depth)
            {
                if(state.GetLength(0) != state.GetLength(1))
                    throw new Exception("Number of columns and rows must be the same");
                
                State = state.Clone() as T[,];
                EmptyRow = emptyRow;
                EmptyCol = emptyCol;
                Depth = depth;

                for (var i = 0; i < State.GetLength(0); i++)
                {
                    for (var j = 0; j < State.GetLength(1); j++)
                        StrRepresentation += State[i, j] + ",";
                }
            }

            public int Size
            {
                get { return State.GetLength(0); }
            }

            public void Print()
            {
                for (var i = 0; i < State.GetLength(0); i++)
                {
                    for (var j = 0; j < State.GetLength(1); j++)
                        Console.Write(State[i,j] + ",");
                    Console.WriteLine();
                }
                Console.WriteLine();
            }

            public int CompareTo(StateNode<T> other)
            {
                if (Value > other.Value)
                    return 1;
                if (Value < other.Value)
                    return -1;

                return 0;
            }
        }

        class AStar<T> where T:IComparable
        {
            public int StatesVisited { get; set; }
            public Dictionary<string, int> PatternDatabase { get; set; }

            private readonly StateNode<T> _goal;
            private T Empty { get; set; }
            private readonly PriorityQueue<StateNode<T>> _queue;
            private readonly HashSet<string> _hash;
            
            public AStar(StateNode<T> initial, StateNode<T> goal,  T empty) 
            {
                _queue = new PriorityQueue<StateNode<T>>(new[] { initial });
                _goal = goal;
                Empty = empty;
                _hash = new HashSet<string>();
            }

            public StateNode<T> Execute()
            {
                _hash.Add(_queue.Min().StrRepresentation);

                while(_queue.Count > 0)
                {
                    var current = _queue.Pop();
                    StatesVisited++;

                    if (current.StrRepresentation.Equals(_goal.StrRepresentation))
                        return current;
                   
                    ExpandNodes(current);
                }

                return null;
            }

            private void ExpandNodes(StateNode<T> node) 
            {
                T temp;
                T[,] newState;
                var col = node.EmptyCol;
                var row = node.EmptyRow;
                StateNode<T> newNode;

                // Up
                if (row > 0)
                {
                    newState = node.State.Clone() as T[,];
                    temp = newState[row - 1, col];
                    newState[row - 1, col] = Empty;
                    newState[row, col] = temp;
                    newNode = new StateNode<T>(newState, row - 1, col,  node.Depth + 1);
                    
                    if (!_hash.Contains(newNode.StrRepresentation))
                    {
                        newNode.Value = node.Depth + Heuristic(newNode);
                        _queue.Push(newNode);
                        _hash.Add(newNode.StrRepresentation);
                    }
                }

                // Down
                if (row < node.Size - 1)
                {
                    newState = node.State.Clone() as T[,];
                    temp = newState[row + 1, col];
                    newState[row + 1, col] = Empty;
                    newState[row, col] = temp;
                    newNode = new StateNode<T>(newState, row + 1, col,  node.Depth + 1);
                    
                    if (!_hash.Contains(newNode.StrRepresentation))
                    {
                        newNode.Value = node.Depth + Heuristic(newNode);
                        _queue.Push(newNode);
                        _hash.Add(newNode.StrRepresentation);
                    }
                }

                // Left
                if (col > 0)
                {
                    newState = node.State.Clone() as T[,];
                    temp = newState[row, col - 1];
                    newState[row, col - 1] = Empty;
                    newState[row, col] = temp;
                    newNode = new StateNode<T>(newState, row, col - 1, node.Depth + 1);
                    
                    if (!_hash.Contains(newNode.StrRepresentation))
                    {
                        newNode.Value = node.Depth + Heuristic(newNode);
                        _queue.Push(newNode);
                        _hash.Add(newNode.StrRepresentation);
                    }
                }

                // Right
                if (col < node.Size - 1)
                {
                    newState = node.State.Clone() as T[,];
                    temp = newState[row, col + 1];
                    newState[row, col + 1] = Empty;
                    newState[row, col] = temp;
                    newNode = new StateNode<T>(newState, row, col + 1, node.Depth + 1);
                    
                    if (!_hash.Contains(newNode.StrRepresentation)) 
                    {
                         newNode.Value = node.Depth + Heuristic(newNode);
                        _queue.Push(newNode);
                        _hash.Add(newNode.StrRepresentation);
                    }
                }
            }

            private double Heuristic(StateNode<T> node)
            {
                return DatabasePattern(node);
            }

            private int MisplacedTiles(StateNode<T> node) 
            {
                var result = 0;
		 
	            for (var i = 0; i < node.State.GetLength(0); i++)
			    {
			        for (var j = 0; j < node.State.GetLength(1); j++)
                        if (!node.State[i, j].Equals(_goal.State[i, j]) && !node.State[i, j].Equals(Empty))
                            result++;    
			    }
	               
                return result;
            }

            private  int ManhattanDistance(StateNode<T> node)
            {
                var result = 0;

                for (var i = 0; i < node.State.GetLength(0); i++)
                {
                    for (var j = 0; j < node.State.GetLength(1); j++)
                    {
                        var elem = node.State[i, j];
                        if (elem.Equals(Empty)) continue;
                        // Variable to break the outer loop and 
                        // avoid unnecessary processing
                        var found = false;
                        // Loop to find element in goal state and MD
                        for (var h = 0; h < _goal.State.GetLength(0); h++)
                        {
                            for (var k = 0; k < _goal.State.GetLength(1); k++)
                            {
                                if (_goal.State[h, k].Equals(elem))
                                {
                                    result += Math.Abs(h - i) + Math.Abs(j - k);
                                    found = true;
                                    break;
                                }
                            }
                            if (found) break;
                        }
                    }
                }

                return result;
            }

            private int LinearConflicts(StateNode<T> node)
            {
                var result = 0;
                var state = node.State;

                // Row Conflicts
                for (var i = 0; i < state.GetLength(0); i++)
                    result += FindConflicts(state, i, 1);

                // Column Conflicts
                for (var i = 0; i < state.GetLength(1); i++)
                    result += FindConflicts(state, i, 0);

                return result;
            }

            private int DatabasePattern(StateNode<T> node)
            {
                var pattern = node.StrRepresentation
                    .Replace('5', '?')
                    .Replace('6', '?')
                    .Replace('7', '?')
                    .Replace('8', '?');

                if (PatternDatabase.ContainsKey(pattern))
                    return PatternDatabase[pattern];
                return ManhattanDistance(node);
            }

            private int FindConflicts(T[,] state, int i, int dimension)
            {
                var result = 0;
                var tilesRelated = new List<int>();

                // Loop foreach pair of elements in the row/column
                for (var h = 0; h < state.GetLength(dimension) - 1 && !tilesRelated.Contains(h); h++)
                {
                    for (var k = h + 1; k < state.GetLength(dimension) && !tilesRelated.Contains(h); k++)
                   {
                        // Avoid the empty tile
                        if (dimension == 1 && state[i, h].Equals(Empty)) continue;
                        if (dimension == 0 && state[h, i].Equals(Empty)) continue;
                        if (dimension == 1 && state[i, k].Equals(Empty)) continue;
                        if (dimension == 0 && state[k, i].Equals(Empty)) continue;

                        var moves = dimension == 1 
                            ? InConflict(i, state[i, h], state[i, k], h, k, dimension) 
                            : InConflict(i, state[h, i], state[k, i], h, k, dimension);
                        
                        if (moves == 0) continue;
                        result += 2;
                        tilesRelated.AddRange(new List<int> { h, k });
                        break;
                    }
                }

                return result;
            }

            private int InConflict(int index, T a, T b, int indexA, int indexB, int dimension)
            {
                var indexGoalA = -1;
                var indexGoalB = -1;

                for (var c = 0; c < _goal.State.GetLength(dimension); c++)
                {
                    if (dimension == 1 && _goal.State[index, c].Equals(a))
                        indexGoalA = c;
                    else if (dimension == 1 && _goal.State[index, c].Equals(b))
                        indexGoalB = c;
                    else if (dimension == 0 && _goal.State[c, index].Equals(a))
                        indexGoalA = c;
                    else if (dimension == 0 && _goal.State[c, index].Equals(b))
                        indexGoalB = c;
                }

                return (indexGoalA >= 0 && indexGoalB >= 0) && ((indexA < indexB && indexGoalA > indexGoalB) ||
                                                                (indexA > indexB && indexGoalA < indexGoalB))
                           ? 2
                           : 0;
            }

        }
    }
}
