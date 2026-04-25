package com.user.management.repository.mongo;

import org.springframework.data.mongodb.repository.MongoRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface UserMongoRepository extends MongoRepository<UserDocument, String> {

    Optional<UserDocument> findByLogin(String login);

    boolean existsByLogin(String login);

    boolean existsByLoginAndIdNot(String login, String id);
}
