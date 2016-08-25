﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;



namespace HandmadeDevil.Core
{
    public static class DomainDefs
    {
        public static readonly string DataKey_GameWrapper = "GameWrapper";
        public static readonly string DataKey_GameState = "GameState";
    }

    public class GameInput
    {
        public KeyboardState keyboardState { get; set; }
        public MouseState mouseState { get; set; }
        // TODO Generalize for several players
        public GamePadCapabilities gamePadCaps { get; set; }
        public GamePadState gamePadState { get; set; }
    }


    // FIXME This object must be allocated only once, on first game run,
    // and maintain its location in memory throughout reloads, so that
    // references pointing to the game state itself keep working!!!

    // FIXME This means that it cannot be contained inside the game assembly!
    // -> Use Managed.Interop to create a big chunk of memory in the hotswapper and pass it to the game instance?
    //    (that would mean somehow abstracting access to unmanaged memory and would require unsafe, also probably very slow)
    // -> Don't allow dangling references to game state and simply serialize the whole object graph normally :P
    [DataContract]
//    [ProtoContract(AsReferenceDefault=true)]
    public class GameState
    {
        [DataMember]
        public int xOffset;
        [DataMember]
        public int yOffset;
        [DataMember]
        public float luminance;
        [DataMember]
        public float luminanceSign;

        [DataMember]
        public double time;

        public GameState()
        {
            luminanceSign = 1f;
        }
    }


    public interface IGameWrapper
    {
        byte[] RetrieveGameStateAndExit();
    }


    public abstract class GameModule : Game
    {
        public GameState gameState { get; private set; }
        public bool isPaused { get; set; }


        public GameModule( byte[] initialState )
        {
            gameState = DeserializeGameState( initialState ) ?? new GameState();
        }

        // TODO Do this using protobuf-net for size/speed! (use AsReference=true for references!)
        // TODO Add ability to dump/load an XML version to disk on key press
        public byte[] SerializeGameState()
        {
            var serializer = new DataContractSerializer( typeof( GameState ) );
            var stream = new MemoryStream();

            using( var writer = XmlDictionaryWriter.CreateBinaryWriter( stream ) )
            {
                serializer.WriteObject( writer, gameState );
            }
            return stream.ToArray();
        }

        private GameState DeserializeGameState( byte[] data )
        {
            if( data == null )
                return null;

            var serializer = new DataContractSerializer( typeof( GameState ) );

            using( var stream = new MemoryStream( data ) )
                using( var reader = XmlDictionaryReader.CreateBinaryReader( stream, XmlDictionaryReaderQuotas.Max ) )
                {
                    return (GameState)serializer.ReadObject( reader );
                }
        }
    }
}