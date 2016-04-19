﻿module Entities
open System.Collections.Generic
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open System.Globalization
open FSharp.Data
open Coroutines
open Utilities
open System
open SnakeDiagram

type Line = A | B | C | D | E
type stationData = JsonProvider<"Samples/StationsAndPlatformsSample.json">
type rideData = JsonProvider<"Samples/RidesAndRideStopsAndPlatformAndStation.json">

type Font = {
    Image : Texture2D
    Data  : FontFile
}

type GameSpeed = {
    Speed : int
    Position : Vector2
} with
    member x.GetSpeed = x.Speed
    static member Create position =
        {  
            Speed = 1
            Position = position
        }
    static member Draw(lastGameSpeed : GameSpeed, font: Font, spriteBatch: SpriteBatch) =
        let fr = new FontRenderer(font.Data, font.Image)
        match lastGameSpeed.GetSpeed with
        | 0 ->  fr.DrawText(spriteBatch, (int)lastGameSpeed.Position.X + 15, (int)lastGameSpeed.Position.Y - 5, "PAUSED")
        | 1 -> fr.DrawText(spriteBatch, (int)lastGameSpeed.Position.X + 15, (int)lastGameSpeed.Position.Y - 5, String.Format("x{0}", lastGameSpeed.GetSpeed))
        | 50 -> fr.DrawText(spriteBatch, (int)lastGameSpeed.Position.X + 15, (int)lastGameSpeed.Position.Y - 5, String.Format("x{0}", lastGameSpeed.GetSpeed))
        | 250 -> fr.DrawText(spriteBatch, (int)lastGameSpeed.Position.X + 15, (int)lastGameSpeed.Position.Y - 5, String.Format("x{0}", lastGameSpeed.GetSpeed))
        | 1000 -> fr.DrawText(spriteBatch, (int)lastGameSpeed.Position.X + 15, (int)lastGameSpeed.Position.Y - 5, String.Format("x{0}", lastGameSpeed.GetSpeed))
        | _ -> ()

    static member Update(lastGameSpeed : GameSpeed) =
        let currentKeyboard = Keyboard.GetState()
        { lastGameSpeed with
            Speed = if currentKeyboard.IsKeyDown(Keys.D1) then 1 
                    else if currentKeyboard.IsKeyDown(Keys.D2) then 50 
                    else if currentKeyboard.IsKeyDown(Keys.D3) then 250 
                    else if currentKeyboard.IsKeyDown(Keys.D4) then 1000 
                    else if currentKeyboard.IsKeyDown(Keys.P) then 0 
                    else lastGameSpeed.GetSpeed
        }

type InfoBox =
    {
        rect    :   Rectangle
        bg      :   Texture2D
        graph   :   SnakeDiagram
    } with
    static member Zero() =
        {
            rect = new Rectangle(0,0,0,0)
            bg = null
            graph = SnakeDiagram.Create (new Rectangle(0,0,0,0)) Vector2.Zero null
        }

    static member Create (textures:  Map<String, Texture2D>) =
        let bg = textures.["InfoBox"]
        let box = new Rectangle(20,20,bg.Width, bg.Height)
        {
            rect = box
            bg = bg
            graph = SnakeDiagram.Create (new Rectangle(30,25,203,74)) (new Vector2(-0.1f, 0.0f)) textures.["metro"]
        }
    
    member this.Draw (spriteBatch: SpriteBatch) (metro: int) (distance: int) =
        spriteBatch.Draw(this.bg, this.rect, Color.White)
        this.graph.Draw(spriteBatch)

    static member Update (infoBox: InfoBox) (gs: GameSpeed) (point: float32) =
        let infobox' = InfoBox.AddPoint infoBox point
        let graph' = SnakeDiagram.Update infobox'.graph gs.GetSpeed
        { infoBox with
            graph = graph'
        }

    static member AddPoint (infobox: InfoBox) (point: float32) =
        {
            infobox with graph = SnakeDiagram.AddPoint infobox.graph point
        }

type RideStop = {
    Name : string
    Arrival : DateTime
    Departure: DateTime
    Position : Vector2
} with
    static member Create(rideStop: rideData.RideStop, scaler: Vector2 -> Vector2, wait: int) =
        {
            Name = rideStop.Platform.Code
            Arrival = rideStop.Time
            Departure = rideStop.Time + (new TimeSpan(0,0,wait))
            Position = scaler (new Vector2((float32)rideStop.Platform.X, (float32)rideStop.Platform.Y))
        }

type Station = {
    Name : string
    Position : Vector2
} with
  member this.Draw(texture: Texture2D, spriteBatch: SpriteBatch) =
        spriteBatch.Draw(texture, new Rectangle((int)this.Position.X - 10, (int)this.Position.Y - 10, 20, 20), Color.White);
  
  static member Create(station: stationData.Value, scaler: Vector2 -> Vector2) =
      {
          Name = station.Name
          Position = scaler (new Vector2((float32)station.X, (float32)station.Y))
      }

type TrainStatus = 
    | Waiting of DateTime
    | Moving of TimeSpan
    | Arrived

type CounterBox = {
    Position : Vector2
    Time : DateTime
} with
    static member Create(position: Vector2, time : DateTime) =
        {
            Position = position
            Time = time
        }
    static member Draw(box: CounterBox, font: Font, texture: Texture2D, spriteBatch: SpriteBatch) = 
        spriteBatch.Draw(texture, new Rectangle((int)box.Position.X, (int)box.Position.Y, 700, 135), Color.Black)
        let fr = new FontRenderer(font.Data, font.Image)
        fr.DrawText(spriteBatch, (int)box.Position.X + 15, (int)box.Position.Y + 15, box.Time.ToString("dddd d MMMM", CultureInfo.CreateSpecificCulture("en-US")))
        fr.DrawText(spriteBatch, (int)box.Position.X + 15, (int)box.Position.Y + 80, box.Time.ToString("HH:mm:ss", CultureInfo.CreateSpecificCulture("en-US")))
        
    static member Update(box: CounterBox, newTime : DateTime) = 
        {
            Position = box.Position
            Time = newTime
        }

type Metro = {
    Line : Line
    RideStops : RideStop list
    Position : Vector2
    Status : TrainStatus
    Behaviour : DateTime -> Coroutine<Unit, Metro> 
} with
    static member Update (dt : DateTime) (x: Metro) =
        let _, metro' = costep (x.Behaviour dt) x
        metro'

    member this.Draw(texture: Texture2D, spriteBatch: SpriteBatch) =
            spriteBatch.Draw(texture, new Rectangle((int)this.Position.X - 2, (int)this.Position.Y - 2, 6, 6), Color.Red)
            ()

    static member Create (line: Line, rideStops: rideData.RideStop array, behaviour: DateTime -> Coroutine<Unit, Metro>) =
        let rideStops' = rideStops |> Array.map(fun x -> RideStop.Create(x, scaler, 0)) |> List.ofArray
        {
            Line = line;
            RideStops = rideStops';
            Position = rideStops'.Head.Position;
            Status = Waiting (rideStops'.Head.Departure)
            Behaviour = behaviour;
        }

let MetroProgram2() (dt : DateTime) : Coroutine<Unit, Metro> =
    let DriveMetro (time : TimeSpan) (dt : DateTime) : Coroutine<bool, Metro> = fun metro ->
        match metro.RideStops with
        | current :: t when not t.IsEmpty ->      
                                let next = t.Head
                                let duration = next.Arrival - current.Arrival

                                let disX = (next.Position.X - current.Position.X)
                                let disY = (next.Position.Y - current.Position.Y)
                                let newPosX = if current.Position.X <> next.Position.X then easeInOutQuad2 ((float32)time.TotalSeconds) current.Position.X disX ((float32)duration.TotalSeconds) else next.Position.X
                                let newPosY = if current.Position.Y <> next.Position.Y then easeInOutQuad2 ((float32)time.TotalSeconds) current.Position.Y disY ((float32)duration.TotalSeconds) else next.Position.Y
                                Done(dt >= next.Arrival , {metro with Position = new Vector2(newPosX, newPosY); Status = Moving ((dt - current.Departure))})
        | _ -> Done(true, metro)

    let inline WaitMetro (departureTime : DateTime) (currentTime : DateTime) : Coroutine<TimeSpan, Metro> = fun metro ->   
        let newTime = departureTime - currentTime
        Done(newTime, metro)

    let inline SetMetroStatus (status : TrainStatus) : Coroutine<Unit, Metro> = fun metro -> 
        Done((), {metro with Status = status})

    let inline SetNextStation (metro : Metro) = 
        match metro.RideStops with
        | h :: t -> Done((), {metro with RideStops = t})
        | _ -> Done((), metro)

    co{
        let! metro = getState
        match metro.Status with
        | Waiting r ->  let! timeRemaining = WaitMetro r dt; 
                        if timeRemaining > TimeSpan.Zero then
                            do! SetMetroStatus (Waiting r)
                            do! yield_
                        else
                            do! SetMetroStatus (Moving (TimeSpan.Zero))
                            return ()
        | Moving t ->   let! arrived = DriveMetro t dt
                        if ((arrived = true) && (metro.RideStops.Length > 0)) then
                            do! SetMetroStatus (Waiting (metro.RideStops.Head.Departure))
                            do! SetNextStation
                            return ()
                        else if ((arrived = true) && (metro.RideStops.Length <= 0)) then
                            do! SetMetroStatus Arrived
                        else
                            do! yield_
        | Arrived ->    return ()
      }

