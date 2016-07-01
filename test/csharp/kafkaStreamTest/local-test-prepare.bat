cd /d D:\app\kafka_2.10-0.10.0.0\bin\windows
kafka-topics.bat --create --zookeeper localhost:2181 --replication-factor 1 --partitions 1 --topic test
kafka-topics.bat --zookeeper localhost:2181 --list
kafka-topics.bat --describe -zookeeper localhost:2181 -topic test
kafka-console-consumer.bat -zookeeper localhost:2181 -from-beginning -topic test
kafka-run-class.bat kafka.admin.TopicCommand -zookeeper localhost:2181 -delete -topic [topic_to_delete] 
