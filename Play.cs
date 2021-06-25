using System;
using Mcts;

public class Play{
    private MctsAI AI;
    private GameInfo gameInfo;
    private TicTacToe tttStateHolder;
    private Piece player_piece;
    private Piece AI_piece;

    private static readonly ConsoleColor player_color = ConsoleColor.Red;
    private static readonly ConsoleColor AI_color = ConsoleColor.Blue;


    static void Main(string[] args){
        Console.WriteLine("Main start\n");

        Play tttPlay = new Play();
        tttPlay.Setup(Piece.O, Piece.X, 3, 3, Piece.X);
        tttPlay.StartGame();

        Console.WriteLine("\nMain end");
    }

    public Play(){

    }

    public void Setup(Piece player_piece, Piece AI_piece, int board_size_X, int board_size_Y, Piece first){
        AI = new MctsAI();
        this.player_piece = player_piece;
        this.AI_piece = AI_piece;

        Piece first_player = first == this.AI_piece ? AI_piece : 
                            first == this.player_piece ? player_piece : Piece.N;

        if(first_player == Piece.N){
            Console.Error.WriteLine(nameof(Setup) + ": first player string may be wrong.");
            return;
        }

        tttStateHolder = new TicTacToe(board_size_X, board_size_Y);

        gameInfo = new GameInfo(
            first_player,
            board_size_X,
            board_size_Y,
            TicTacToe.GetInitBoard(board_size_X, board_size_Y),
            null // because it's initial state
        );
    }

    public void StartGame(){
        tttStateHolder.PrintBoard();

        float play_result;
        while(true){
            if(gameInfo.Player == player_piece){
                play_result = PlayByPlayer();
                if(play_result == 1){ // player wins
                    EndGame(player_piece);
                    break;
                }else if(play_result == 0.5f){ // draw
                    EndGame(null);
                    break;
                }
            }else if(gameInfo.Player == AI_piece){
                play_result = PlayByAI();
                if(play_result == 1){ // AI wins
                    EndGame(AI_piece);
                    break;
                }else if(play_result == 0.5f){ // draw
                    EndGame(null);
                    break;
                }
            }
        }
    }

    private void EndGame(Piece? winner){
        if(winner == null){
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Draw game");
            Console.ResetColor();
        }else{
            Console.ForegroundColor = (Piece)winner == AI_piece ? AI_color : player_color;
            Console.WriteLine("Winner: " + winner.ToString());
            Console.ResetColor();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>1: player wins, 0.5: draw, -1: continue</returns>
    private float PlayByPlayer(){
        Move player_move;
        Piece? winner;

        while(true){
            player_move = GetPlayerMove();
            // if player move is valid, go on next
            if(tttStateHolder.PlacePiece(player_move.x, player_move.y, player_move.piece)){
                break;
            }
        }
        // is there winner ? show board
        Console.ForegroundColor = player_color;
        Console.WriteLine("You: ({0}, {1}), {2}", player_move.x, player_move.y, player_move.piece);
        Console.ResetColor();
        tttStateHolder.PrintBoard();
        winner = tttStateHolder.GetWinner();
        if(winner == player_piece){
            return 1;
        }
        else if(tttStateHolder.IsFullBoard()){
            return 0.5f;
        }

        UpdateGameInfo(AI_piece, tttStateHolder.BoardString, player_move);

        return -1;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>1: AI wins, 0.5: draw, -1: continue</returns>
    private float PlayByAI(){
        Move AI_move;
        Piece? winner;

        AI_move = GetAIMove(gameInfo, 100000);
        tttStateHolder.PlacePiece(AI_move.x, AI_move.y, AI_move.piece);
        // is there winner ? show board
        Console.ForegroundColor = AI_color;
        Console.WriteLine("AI: ({0}, {1}), {2}", AI_move.x, AI_move.y, AI_move.piece);
        Console.ResetColor();
        tttStateHolder.PrintBoard();
        winner = tttStateHolder.GetWinner();
        if(winner == AI_piece){
            return 1;
        }
        else if(tttStateHolder.IsFullBoard()){
            return 0.5f;
        }

        UpdateGameInfo(player_piece, tttStateHolder.BoardString, AI_move);

        return -1;
    }

    private Move GetPlayerMove(){
        string[] move_pos;
        while(true){
            Console.Write("What's your move? x y: ");
            move_pos = Console.ReadLine().Split();
            if(move_pos.Length == 2){
                if(Int32.TryParse(move_pos[0], out int x) && Int32.TryParse(move_pos[1], out int y) &&
                    x < gameInfo.Board_size_X && y < gameInfo.Board_size_Y){
                    return new Move(
                        x, y, player_piece
                    );
                }
            }
        }
    }

    private Move GetAIMove(GameInfo gameInfo, int iterate_count){
        return AI.Think(gameInfo, iterate_count);
    }

    private void UpdateGameInfo(Piece next_player, string boardString, Move move){
        gameInfo.Player = next_player;
        gameInfo.BoardString = boardString;
        gameInfo.PreMove = move;
    }


}

public class Move{
    public int x;
    public int y;
    public Piece piece;

    public Move(int x, int y, Piece piece){
        this.x = x;
        this.y = y;
        this.piece = piece;
    }
}