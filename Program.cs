using System;
using System.Threading;
namespace ParkingSpotDistribution {
    public delegate void priceCutEvent(Int32 pr); // Define a delegate for price cut event.
    public delegate void orderPlaced();  // Define a delegate for handling process order event. Order process event will be triggered whenever a onew object is inserted into a cell. 
    
    // Class definition for Parking Structure.
    public class ParkingStruture {
        public static Random rand = new Random(); 
        public static event priceCutEvent priceCut; // Declaring a event with delegate. Will be used to call the handler functions.
        private static Int32 parkingSpotPrice = 10; // Initializing the parkingspot price with 10.
        private static Int32 numPriceCuts = 0; // Count variable to keep note of number of price cuts.
        
        public void parkingStructureFunc()  // Parking Structure thread function
        {
            // Run the parking thread till price cut happens 20 times. 
            while (numPriceCuts < 20) {
                // Making Parking thread sleep for 500 milli seconds before changing price each time. 
                Thread.Sleep(500);
                // Utilize pricing model to get the new price.
                Int32 p = ParkingStruture.PricingModel();
                // Call change price function with new price 
                ParkingStruture.changePrice(p);
            }
            // Set the parking structure thread flag to be false. This will be used by Parking agent to stop the respective threads. 
            myApplication.parkingStructureThread = false;
        }
        // Getter function to get current parking spot price.
        public static Int32 getPrice() { 
            return parkingSpotPrice; 
        }
        // Price changing function and Price cut event triggers here.
        public static void changePrice(Int32 price)  { 
            if (price < parkingSpotPrice)
            {
                if (priceCut != null) { 
                    // call event handlers
                    priceCut(price);
                    // increment price cuts counter. 
                    Console.WriteLine("Price cut event called, total number of price cuts completed till now - {0}",numPriceCuts+1);
                    numPriceCuts++;
                } 
            }
            // update the current parking spot price with changes price.
            parkingSpotPrice = price;
        }
        // Pricing model that generates random numbers between (10, 40)
        public static Int32 PricingModel()  {
            Int32 randPrice;
            Random rnd = new Random();
            // Generate random numbers between 10 and 40 to update the parking spot price.
            randPrice = rnd.Next(10, 40);
            return randPrice;
        }
        // Defining Order Processing function that will be started as a separate thread to execute the order placed by parking agent.
        public static void orderProcessing(OrderClass nxtOrder) { 
            // Credit Card Validation
            if (nxtOrder.creditCardNo < 4000 || nxtOrder.creditCardNo > 6000) {
                Console.WriteLine("Order by Sender {0} and {1} no of units is cancelled as credit card number validation failed", nxtOrder.senderID, nxtOrder.quantity);
            }
            else {
                // generate a randome tax value between 8 and 12 percent
                Int32 tax = rand.Next(8,12);
                // generate a random location charget between 2 and 8 percent
                Int32 locationCharge = rand.Next(2,8);

                double parkingSpotsValue = (nxtOrder.quantity*nxtOrder.unitPrice);
                // Calculate the net bill amount using unit price, quantity, tax and location charge
                double bill_value = parkingSpotsValue + (parkingSpotsValue*tax*0.01) + (parkingSpotsValue*locationCharge*0.01);
                // Call order confirmation function along with sender id so that parking agents will get to know that order is processed.
                ParkingAgent.orderConfirmation(nxtOrder.senderID, bill_value, nxtOrder.unitPrice);
            }
        }
        // Handler function for orderPlaced event. Whenever a new order is placed in MultiCellBuffer Agent will trigger this event so that order processing will be triggered from here.
        public void pickNextOrder() { // Event Handler
            OrderClass nxtOrder = myApplication.buffer.getOneCell();
            Thread thread = new Thread(() => ParkingStruture.orderProcessing(nxtOrder)); 
            thread.Start();
        }
    } 
    // Order class containing all the contents of an order.
    public class OrderClass {
        // Declaring order contents along with getter and setter properties.
        public Int32 creditCardNo { get; set; }
        public Int32 quantity { get; set; }
        public double unitPrice { get; set; }
        public string senderID { get; set; }
        public string receiverID { get; set; }
        // Constructor to initialize or create an order object. 
        public OrderClass (Int32 ccNO, Int32 quant, double unit_price, string sender, string receiver) {
            this.creditCardNo = ccNO;
            this.quantity = quant;
            this.unitPrice = unit_price;
            this.senderID = sender;
            this.receiverID = receiver;
        }
    }   

    // Define Parking Agent class
    public class ParkingAgent {
        // Declaring orderPlaced event which will be used to class orderProcessing once a new order is placed into buffer cell.
        public static event orderPlaced orderPlacedEvent;
        static Random rand = new Random(); 
        // Initializing parking spot prioce with 10. This will be updated on every price cut.
        public static Int32 currentParkingSpotPrice = 10;
        // Definig a bool to indicate if price cut happened.
        public bool priceCut = false;
        // Defining a budget per order this will be used to calculate order quantity.
        public static Int32 budgetPerOrder = 200;
        // Initializing credit card.
        public Int32 creditCard = rand.Next(4000, 6000);
        // Paring agent function which will be run as a thread. This thread would place order every 1000 milli seconds.
        public void parkingAgentThread() {   //for starting thread 
            while (myApplication.parkingStructureThread) {
                // Putting thread to sleep for 1000 milliseconds.
                Thread.Sleep(1000);
                // If price cut has occured use the updated price for placing order.
                if(priceCut) {
                    placeOrder(currentParkingSpotPrice);
                    priceCut = false;
                }
                else {
                    // If no price cut, use the current price in parking structure for placing order.
                    placeOrder(ParkingStruture.getPrice());
                }
            }
        }
        // Place order function to create order object and trigger the order processing event in ParkingStructure.
        public void placeOrder(Int32 p)  {
            currentParkingSpotPrice = p;
            // Calculate number of units based on unit price and budget per order.
            Int32 noOfUnits =(int) Math.Ceiling((double)(budgetPerOrder/p));        
            // Create order object with all the contents.
            OrderClass oc = new OrderClass(creditCard, noOfUnits, currentParkingSpotPrice, Thread.CurrentThread.Name, "ParkingStructureThread");
            // Call setonecell function to push order object into buffer cell.
            myApplication.buffer.setOneCell(oc);
            Console.WriteLine("Order placed by Sender{0} for price {1} for {2} number of units", Thread.CurrentThread.Name, currentParkingSpotPrice, noOfUnits);
            // Once the order is placed, trigger order processing in parking structure.
            orderPlacedEvent();
        }
        // Defining order confirmation function, this will be called once the order is processed by orderprocessing thread.
        public static void orderConfirmation(string senderID, double bill, double price) {
            Console.WriteLine("Order processed for sender {0}. Total Bill was {1}, price used {2}", senderID, bill, price);
        }
        // Defining price cut handler, once a price cut happens this gets triggered and the price get updated in the class variable.
        public void priceCutEventHandler(Int32 p)  {
            Console.WriteLine("ParkingAgent{0}: Parking spots are on sale: as low as ${1} per spot", Thread.CurrentThread.Name, p); 
            // Updating the class variable with new price cut price.
            currentParkingSpotPrice = p;
            // Setting the price cut boolean true. So that when agent tries to place order, it will use the price-cut price for placing order.
            priceCut = true;
        }
    }   

    // Defining the multi cell buffer
    public class MultiCellBuffer {
        // Defining Buffer cells with each cell containing OrderClass object.
        public static OrderClass[] dataCells;
        // Defining read semaphores.
        public static Semaphore readSemaphore;
        // Declaring write semaphores.
        public static Semaphore writeSemaphore; 
        // Cell availability variable to use as a pointer to indicate how many cells are filled currently.
        public static Int32 cellAvailability = 0;
        
        public MultiCellBuffer (Int32 n) {
            // initializing read semaphore with 0 and maximum value passed through the constructor.
            readSemaphore = new Semaphore(initialCount: 0, maximumCount: n);
            // initializing write semaphore with 0 and maximum value passed through the constructor.
            writeSemaphore = new Semaphore(initialCount: 0, maximumCount: n);
            // Releasing all semaphores to let threads access the cells.
            readSemaphore.Release(n);
            writeSemaphore.Release(n);
            // Initializing the three buffer cells.
            dataCells = new OrderClass[n];
        }
        // Defining Set One cell function.
        public void setOneCell(OrderClass newObj) {
            // Adding Waitone to make sure excess threads to wait till the semaphores gets freed.
            writeSemaphore.WaitOne();
            // Locking the buffer cells till current thread finishes transaction
            lock(this) {
                // Put agent to wait of the buffer is full.
                while (cellAvailability >= 2) {
                    Console.WriteLine("Agent {0} - waiting to write to buffer",newObj.senderID);
                    Monitor.Wait(this);
                }
                // Insert an object to data cells.
                dataCells[cellAvailability] = newObj;
                // after inserting, if the buffer is full reset the count variable to 0.
                if(cellAvailability == 2) {
                    cellAvailability = 0;
                }
                else {
                    // If the buffer is not full increment by 1.
                    cellAvailability = cellAvailability + 1;
                }
                Console.WriteLine("Agent {0} - successfully written to buffer",newObj.senderID);
                // Release write semaphore.
                writeSemaphore.Release();
                Monitor.Pulse(this);
            }
            }   
        // Defining getOneCell function.
        public OrderClass getOneCell() { 
            // Adding Waitone to make sure excess threads to wait till the semaphores gets freed.
            readSemaphore.WaitOne();
            // Creating a OrderClass variable. This will be filled with order object in next few lines.
            OrderClass order = null;    
            // Locking the buffer cells till current thread finishes transaction
            lock(this) {
                // If the buffer is empty. Wait till order gets inserted by agents.
                while (MultiCellBuffer.cellAvailability <= 0) {
                    Monitor.Wait(this);
                }
                // Get a new order from buffer.
                for (int i = 0; i < MultiCellBuffer.dataCells.Length; i++) {
                if (MultiCellBuffer.dataCells[i]!=null) {
                    order = MultiCellBuffer.dataCells[i];
                    break;
                }
                }
                // Decrement the count of occupied cells in buffer.
                MultiCellBuffer.cellAvailability = MultiCellBuffer.cellAvailability - 1;
                // Release semaphores.
                readSemaphore.Release();
                Monitor.Pulse(this);
            }
            return order;
        }
    }

    public class myApplication {
        // Defining parkingStructureThread boolean variable, which will be used by agent to stop itself after 20 price cuts of parking structure thread.
        public static bool parkingStructureThread = true; 
        // Initializing the buffer cells.
        public static MultiCellBuffer buffer = new MultiCellBuffer(3);
            static void Main(string[] args)  {
                // Giving number of agents to be 5 and parking structure threads to be 1.
                Int32 N = 5;
                Int32 k = 1;
                Thread[] parkingStructureThreads = new Thread[k];
                ParkingStruture[] parking_structure = new ParkingStruture[k];
                for (int i = 0; i < k; i++)
                {
                    // Creating an instance of ParkingStructure.
                    parking_structure[i] = new ParkingStruture();
                    // Crearing parking structure threads.
                    parkingStructureThreads[i] = new Thread(new ThreadStart(parking_structure[i].parkingStructureFunc)); 
                    // Naming parking structure thread.
                    parkingStructureThreads[i].Name = "ParkingStructure_Thread"+((i + 1).ToString());
                    // Starting parking structure thread.
                    parkingStructureThreads[i].Start();
                    // Subscribing orderPlaced event of ParkingAgent with pickNextOrder event handler in parking structure class to perform order processing whenever a new order is pushed to buffer cell.
                    ParkingAgent.orderPlacedEvent += new orderPlaced(parking_structure[i].pickNextOrder);
                }
                
                Thread[] parkingAgentThreads = new Thread[5];
                ParkingAgent[] parking_agent = new ParkingAgent[5];
                for (int i = 0; i < N; i++)
                {
                    // Creating an instance of ParkingAgent.
                    parking_agent[i] = new ParkingAgent();
                    // Subscribing price cut event of parking structure with priceCutEventHandler event handler in parking agent class.
                    ParkingStruture.priceCut += new priceCutEvent(parking_agent[i].priceCutEventHandler);
                    // Crearing parking agent threads.
                    parkingAgentThreads[i] = new Thread(new ThreadStart(parking_agent[i].parkingAgentThread));
                    // Naming parking agent thread.
                    parkingAgentThreads[i].Name = "Agent"+((i + 1).ToString());
                    // Starting parking agent thread.
                    parkingAgentThreads[i].Start();
                }
            }
    }
}