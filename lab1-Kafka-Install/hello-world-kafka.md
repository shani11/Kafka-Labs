In this lab we will install Kafka with Docker and verify it is working by creating a topic and sending some messages.

1. Install Kafka using Docker

    - Create docker-compose.yml that have two service conatiners zookeeper , kafka
    - Run the Kafka using $ docker-compose up
    - You can stop the Kafka and Zookeeper servers with Docker Compose: docker-compose down

2. Create a topic called helloworld with a single partition and one replica:
    - docker-compose exec kafka kafka-topics.sh --bootstrap-server :9092 --create --replication-factor 1 --partitions 1 --topic helloworld
    - ou can now see the topic that was just created with the --list flag: docker-compose exec kafka kafka-topics.sh --bootstrap-server :9092 --list

    image.png

3. Producer using : docker-compose exec kafka kafka-console-producer.sh --bootstrap-server :9092 --topic helloworld

4. Consumer using :  docker-compose exec kafka kafka-console-consumer.sh --bootstrap-server kafka:9092 --topic helloworld --from-beginning