using System;
using System.Collections.Generic;

namespace Mcts
{
    public class Mcts
    {
        private readonly int EXPANDER_THRESHOLD = 10;
        private Node root;
        private int totalPlayout;
        private string selectionFormula;

        static void Main(string[] args)
        {
            Console.WriteLine("Main start\n");

            int board_size_X = 3;
            int board_size_Y = 3;

            TicTacToe ttt = new TicTacToe(board_size_X, board_size_Y);
            ttt.PlacePiece(0, 0, Piece.O);
            ttt.PlacePiece(1, 2, Piece.X);

            ttt.PrintBoard("root");

            Mcts mcts = new Mcts(Node.UCT);
            mcts.SetRoot(
                new GameInfo(
                    player:Piece.O,
                    board_size_X,
                    board_size_Y,
                    ttt.Board2String(ttt.Board)
                )
            );
            mcts.RunAllSteps();

            Console.WriteLine("\nMain end");
        }

        public Mcts(string selectionFormula){
            this.selectionFormula = selectionFormula;
            totalPlayout = 0;
        }

        public void SetRoot(GameInfo gameInfo){
            root = new Node(gameInfo);
            root.SetSelectionFormula(selectionFormula);
            root.SetPossibleChildren();
        }

        public void RunAllSteps(){
            Select();
            Expand();
            bool is_win = Simulate();
            BackPropagate(is_win);
        }

        private Node target_leaf; // used among four steps below

        private void Select(){
            Node child = root;
            while(!child.IsLeaf()){
                child = child.SelectChild(totalPlayout);
            }
            target_leaf = child;
            string bs = target_leaf.GameInfo.BoardString;
            TicTacToe ttt = new TicTacToe(3, 3);
            ttt.SetBoard(ttt.String2Board(bs));
            ttt.PrintBoard("--------");
        }

        private void Expand(){
            if(target_leaf.PlayoutCount > EXPANDER_THRESHOLD){
                target_leaf.SetPossibleChildren();
                target_leaf = target_leaf.SelectChild(totalPlayout);
            }
        }

        private bool Simulate(){
            totalPlayout++;
            return target_leaf.Playout();
        }

        private void BackPropagate(bool is_win){
            Node updating_node = target_leaf;
            while(!updating_node.IsRoot()){
                updating_node.UpdateInfo(is_win);
                is_win = !is_win;
                updating_node = updating_node.Parent;
            }
        }

        
    }

    class Node{
        private int playoutCount;
        public int PlayoutCount{
            private set { this.playoutCount = value; }
            get { return this.playoutCount; }
        }
        private int winCount;
        public int WinCount{
            private set { this.winCount = value; }
            get { return this.winCount; }
        }

        private Node parent = null;
        public Node Parent{
            private set { this.parent = value; }
            get { return this.parent; }
        }
        private List<Node> children = new List<Node>();
        public List<Node> Children{
            private set { this.children = value; }
            get { return this.children; }
        }

        private string selectionFormula;
        static public readonly string UCT = nameof(UCT);

        // TicTacTie related start ------->
        public GameInfo GameInfo{ private set; get; }
        //  <------- TicTacTie related end

        public Node(){
            playoutCount=0;
        }

        public Node(GameInfo gameInfo){
            this.GameInfo = gameInfo;
            playoutCount=0;
        }

        /// <summary>
        /// Reuturn true if this node is a root.
        /// </summary>
        /// <returns></returns>
        public bool IsRoot(){
            return parent == null;
        }

        /// <summary>
        /// Return true if this node is a leaf.
        /// </summary>
        /// <returns></returns>        
        public bool IsLeaf(){
            return children.Count == 0;
        }

        /// <summary>
        /// Set the formula such as UCT that are used for selecting a child node.
        /// </summary>
        /// <param name="selectionFormula"></param>
        public void SetSelectionFormula(string selectionFormula){
            this.selectionFormula = selectionFormula;
        }
 
        /// <summary>
        /// Select a child by using selection formula.
        /// </summary>
        /// <param name="totalPlayout"></param>
        /// <returns></returns>
        public Node SelectChild(int totalPlayout){
            if(selectionFormula == null){
                selectionFormula = parent.selectionFormula;
            }

            if(selectionFormula == UCT){
                // uctでえらぶ
                float maxUCT = float.MinValue;
                Node chosenChild = new Node();
                foreach(var child in children){
                    float UCT = child.GetUCT(totalPlayout);
                    if(UCT > maxUCT){
                        chosenChild = child;
                        maxUCT = UCT;
                    }
                }
                return chosenChild;
            }else{
                Console.Error.WriteLine(nameof(SelectChild) + "invalid selectionFormula.");
                return null;
            }
        }

        /// <summary>
        /// Set the formula and then select a child by using selection formula.
        /// </summary>
        /// <param name="totalPlayout"></param>
        /// <param name="selectionFormula"></param>
        /// <returns></returns>
        public Node SelectChild(int totalPlayout, string selectionFormula){
            this.selectionFormula = selectionFormula;
            return SelectChild(totalPlayout);
        }

        /// <summary>
        /// Set all possible children to this node.
        /// </summary>
        public void SetPossibleChildren(){
            // TicTacToe related function
            TicTacToe ttt = new TicTacToe(GameInfo.Board_size_X, GameInfo.Board_size_Y);
            var nextBoardStrs = ttt.GetAllNextBoardStrs(GameInfo.BoardString, GameInfo.Player);
            foreach(var nextBoard in nextBoardStrs){
                GameInfo childGameInfo = new GameInfo(
                    TicTacToe.GetNextPlayer(GameInfo.Player),
                    GameInfo.Board_size_X,
                    GameInfo.Board_size_Y,
                    nextBoard
                );
                Node child = new Node(childGameInfo);
                Children.Add(child);
            }
        }

        /// <summary>
        /// Perform a playout and return the result.
        /// </summary>
        /// <returns></returns>
        public bool Playout(){
            return true;
        }

        /// <summary>
        /// Update information about the number of wins and playouts.
        /// </summary>
        /// <param name="is_win"></param>
        public void UpdateInfo(bool is_win){

        }

        /// <summary>
        /// Calculate UCT.
        /// </summary>
        /// <remarks>
        /// This method may return a random value, 
        /// which means every time you call, different value may come even from the same node.
        /// </remarks>
        public float GetUCT(int totalPlayout){
            float c = 1.414f;
            // I don't know what should be UCT when no playout has happened yet
            // here it returns random value that is definitetly larger than normal case(below)
            if(playoutCount == 0){
                Random r = new Random();
                return c*totalPlayout*totalPlayout + (float)r.NextDouble();
            }else{
                float reword = (float)winCount / playoutCount;
                float bias = (float)(c * Math.Pow(Math.Log(totalPlayout) / playoutCount, 0.5));
                return reword + bias;
            }
        }
        
    }

    public class GameInfo{
        public Piece Player{ private set; get; }
        public int Board_size_X{ private set; get; } // must be 2 or more
        public int Board_size_Y{ private set; get; } // must be 2 or more
        public string BoardString{ set; get; }

        public GameInfo(Piece player, int board_size_X, int board_size_Y, string boardString){
            Player = player;
            Board_size_X = board_size_X;
            Board_size_Y = board_size_Y;
            BoardString = boardString;
        }
    }
    
}
