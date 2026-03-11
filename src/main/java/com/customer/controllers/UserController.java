package com.customer.controllers;

import com.customer.models.User;
import com.customer.repository.UserRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import java.util.UUID;


@RestController
@RequestMapping("/test_users")
public class UserController {
    @Autowired
    private UserRepository userRepository;

    @PostMapping
    public ResponseEntity<User> createUser(@RequestBody User user){
        if (user.getId() == null || user.getId().isEmpty()){
            user.setId(UUID.randomUUID().toString());
        }

        User saved = userRepository.save(user);
        return 
        ResponseEntity.status(HttpStatus.CREATED).body(saved);
    }
}
