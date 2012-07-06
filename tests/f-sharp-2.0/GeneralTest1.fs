#light

// Simple F# program to exercise fsc.exe

type Season = 
    | Spring 
    | Summer 
    | Fall 
    | Winter
    override this.ToString() =
        match this with
        | Spring -> "Spring"
        | Summer -> "Summer"
        | Fall   -> "Fall"
        | Winter -> "Winter"

let genSeasonTuples monthRange season =
    monthRange |> List.map (fun monthInd -> (monthInd, season))

let monthSeasonList =   (genSeasonTuples  [1 .. 3]  Spring) 
                      @ (genSeasonTuples  [4 .. 6]  Summer) 
                      @ (genSeasonTuples  [7 .. 10] Fall) 
                      @ (genSeasonTuples [11 .. 12] Winter)
let monthSeasonMap = Map.FromList monthSeasonList

open System

let currentMonth = DateTime.Now.Month
let currentSeason = monthSeasonMap.[currentMonth]

printfn "Current season = %s" (currentSeason.ToString())
//Console.WriteLine("(press any key)")
//Console.ReadKey(true)
