using System;
using System.Collections.Generic;
using System.Text;

using BOUNLib.Messages;
using System.Threading;

namespace BOUNLib
{
    namespace Messaging
    {
        /// <summary>
        /// The interface definition for source classes. 
        /// </summary>
        public abstract class MsgInitiator : IDisposable
        {
            protected MsgService msgService;
            public MsgInitiator(MsgService msgService)
            {
                this.msgService = msgService;
                this.msgService.init(null);
            }
            /// <summary>
            /// Starts the information flow.
            /// </summary>
            public abstract void start();
            
            /// <summary>
            /// Stops the information flow.
            /// </summary>
            public abstract void stop();

            public virtual void Dispose()
            {
                msgService.Dispose();
            }
        }

        /// <summary>
        /// The interface definition for processor classes.
        /// </summary>
        public abstract class MsgConsumer : IDisposable
        {
            protected MsgService msgService;
            protected Type msgType;
            public MsgConsumer(MsgService msgService, Type msgType)
            {
                this.msgService = msgService;
                this.msgType = msgType;
                if (this.msgService != null)
                    this.msgService.init(this);
            }

            /// <summary>
            ///  Returns the message type that the processor handles.
            /// </summary>
            /// <returns></returns>
            public Type getMsgType() 
            {
                return msgType;
            }

            /// <summary>
            /// Consumes the provided message.
            /// </summary>
            /// <param name="message"></param>
            /// <param name="msgID"></param>
            public abstract void consumeMessage(IMessage message, int msgID);
            public virtual void Dispose()
            {
                msgService.Dispose();
            }
        }

        /// <summary>
        /// The base message service class.
        /// </summary>
        public abstract class MsgService : IDisposable
        {
            protected MsgConsumer consumer;
            protected int msgID;
            public MsgService()
            {
                msgID = 1;
            }
            public virtual void init(MsgConsumer consumer) 
            {
                this.consumer = consumer;
            }

            /// <summary>
            /// Send message to the target.
            /// </summary>
            /// <param name="msg"></param>
            public abstract void sendMsg(IMessage msg);

            /// <summary>
            /// Receive message from source.
            /// </summary>
            /// <param name="msg"></param>
            public virtual void receiveMsg(byte[] msg)
            {

                IMessage message = Activator.CreateInstance(consumer.getMsgType()) as IMessage;
                message = message.fromByteArray(msg);
                if (message.isValid())
                {
                    consumer.consumeMessage(message, msgID++);
                }
                else
                {
                    Console.WriteLine("Invalid message: " + message);
                }
            }
            public virtual void Dispose()
            {
               
            }
        }

        /// <summary>
        /// Simply transfers the acquired message to the target object.
        /// </summary>
        public class BasicMsgService:MsgService
        {
            MsgConsumer target;
            public BasicMsgService(MsgConsumer target)
            {
                this.target = target;
            }
            public override void sendMsg(IMessage msg)
            {
                if (target != null && target.getMsgType().Equals(msg.GetType()))
                    target.consumeMessage(msg, msgID++);
            }

        }

        /// <summary>
        /// Creates a copy of the acquired message for all registered target objects.
        /// </summary>
        public class DispatchMsgService : MsgService
        {
            MsgConsumer[] target;

            public DispatchMsgService(MsgConsumer[] target)
            {
                if (target == null || target.Length == 0)
                {
                    throw new ArgumentNullException("target");
                }
                this.target = target;
            }
            
            public override void sendMsg(IMessage msg)
            {

                for (int i = 0; i < target.Length; i++)
                {
                    if (target[i].getMsgType().Equals(msg.GetType()))
                    {
                        target[i].consumeMessage(msg.duplicate(), msgID++);
                    }
                }
            }
        }

        /// <summary>
        /// The main message flow binding object.
        /// </summary>
        public class MessageFlow
        {
            List<MsgConsumer> consumers;
            List<MsgInitiator> initiators;

            public MessageFlow()
            {
                consumers = new List<MsgConsumer>();
                initiators = new List<MsgInitiator>();
            }

            /// <summary>
            /// Register consumers.
            /// </summary>
            /// <param name="item"></param>
            public void addConsumer(MsgConsumer item)
            {
                consumers.Add(item);
            }

            /// <summary>
            /// Register sources.
            /// </summary>
            /// <param name="item"></param>
            public void addInitiator(MsgInitiator item)
            {
                initiators.Add(item);
            }

            /// <summary>
            /// Triggers the sources to start.
            /// </summary>
            public void startFlow()
            {
                foreach (MsgInitiator s in initiators)
                {
                    s.start();
                }
            }


            /// <summary>
            /// Stops the flow.
            /// </summary>
            public void stopFlow()
            {
                foreach (MsgInitiator s in initiators)
                {
                    if (s != null)
                    {
                        s.stop();
                        s.Dispose();
                    }
                }
                foreach (MsgConsumer p in consumers)
                {
                    if (p!=null)
                        p.Dispose();
                }
            }
        }
    }
}
